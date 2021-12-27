using GalaSoft.MvvmLight;
using Stride.ShaderParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Serialization;

namespace StrideShaderExplorer
{
    public enum StrideSourceDirMode
    {
        Official,
        Dev
    }

    public class MainViewModel : ViewModelBase
    {
        private const string StrideEnvironmentVariable = "StrideDir";
        private const string NugetEnvironmentVariable = "NUGET_PACKAGES";

        private string _filterText;
        private string _selectedWord;
        private IReadOnlyList<string> _paths;

        public List<string> AdditionalPaths
        {
            get;
            set;
        }

        /// <summary>
        /// The list of roots of the tree view. This includes all the shaders
        /// that do not inherit from any other shaders.
        /// </summary>
        public List<ShaderViewModel> RootShaders { get; set; }

        /// <summary>
        /// The list of all shaders.
        /// </summary>
        public IEnumerable<ShaderViewModel> AllShaders => ShadersInPostOrder();

        public string FilterText
        {
            get { return _filterText; }
            set
            {
                if (Set(ref _filterText, value))
                    UpdateFiltering();
            }
        }

        public string SelectedWord
        {
            get { return _selectedWord; }
            set
            {
                if (Set(ref _selectedWord, value))
                {
                }
            }
        }

        private string ResolveNugetPackageDir()
        {
            var nugetPackageDir = Environment.GetEnvironmentVariable(NugetEnvironmentVariable);
            if (nugetPackageDir != null)
            {
                return nugetPackageDir;
            }
            else
            {
                var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userDir, ".nuget", "packages");
            }
        }

        internal void Refresh()
        {
            try
            {
                List<string> paths = null;
                switch (StrideDirMode)
                {
                    case StrideSourceDirMode.Official:
                        var nugetPackageDir = ResolveNugetPackageDir();
                        var directories = Directory.GetDirectories(nugetPackageDir) //package dir
                            .Where(dir => Path.GetFileName(dir).StartsWith("stride", StringComparison.OrdinalIgnoreCase)) //stride folders
                            .Where(dir => Directory.EnumerateFileSystemEntries(dir).Any())
                            .Select(dir => Directory.GetDirectories(dir).Where(subdir => !subdir.EndsWith("-dev")) //exclude local build package
                            .OrderBy(subdir2 => subdir2, StringComparer.OrdinalIgnoreCase).LastOrDefault()); //latest version
                        paths = directories.ToList();
                        break;
                    case StrideSourceDirMode.Dev:
                        var strideDir = Environment.GetEnvironmentVariable(StrideEnvironmentVariable);
                        if (strideDir != null)
                        {
                            paths = new List<string> { strideDir };
                        }
                        else
                        {
                            var dialog = new System.Windows.Forms.FolderBrowserDialog();
                            dialog.Description = "\"StrideDir\" environment variable not found. Select source repo main folder manually.";
                            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                paths = new List<string> { dialog.SelectedPath };
                            }
                            //basePath = System.IO.Path.Combine(basePath, "sources", "engine", "Stride.Engine", "Rendering");
                        }
                        break;
                    default:
                        break;
                }

                paths.AddRange(AdditionalPaths);

                Paths = paths;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal void SaveUserSetting()
        {
            Properties.UserSettings.Default.AdditionalPaths = string.Join(";", AdditionalPaths);
            Properties.UserSettings.Default.Save();
        }

        /// <summary>
        /// Path to the Stride installation folder.
        /// </summary>
        public IReadOnlyList<string> Paths
        {
            get { return _paths; }
            set
            {
                if (Set(ref _paths, value))
                {
                    try
                    {
                        RootShaders = BuildShaderTree().OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
                        RaisePropertyChanged(nameof(RootShaders));
                        RaisePropertyChanged(nameof(AllShaders));
                        UpdateFiltering();
                        ExpandAll(false);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        public StrideSourceDirMode StrideDirMode { get; internal set; }

        public MainViewModel()
        {
            AdditionalPaths = Properties.UserSettings.Default.AdditionalPaths.Split(';').ToList();
            Refresh();
        }

        private void UpdateFiltering()
        {
            foreach (var shader in AllShaders)
            {
                shader.IsVisible = string.IsNullOrEmpty(_filterText) ||
                    shader.Name.ToLower().Contains(_filterText.ToLower());
                shader.IsExpanded = shader.DerivedShaders.Any(o => o.IsVisible);
            }
        }

        public void ExpandAll(bool expand)
        {
            foreach (var shader in AllShaders)
                shader.IsExpanded = expand;
        }

        private IEnumerable<ShaderViewModel> ShadersInPostOrder()
        {
            foreach (var rootShader in RootShaders)
                foreach (var shader in ShadersInPostOrder(rootShader))
                    yield return shader;
        }

        private static IEnumerable<ShaderViewModel> ShadersInPostOrder(ShaderViewModel shader)
        {
            foreach (var child in shader.DerivedShaders)
                foreach (var s in ShadersInPostOrder(child))
                    yield return s;
            yield return shader;
        }

        private IEnumerable<ShaderViewModel> BuildShaderTree()
        {
            var files = Paths.Where(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
                .SelectMany(path => Directory.GetFiles(path, "*.sdsl", SearchOption.AllDirectories));
            var shaders = new Dictionary<string, ShaderViewModel>();
            var duplicates = new Dictionary<string, ShaderViewModel>();

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if(!shaders.ContainsKey(name))
                    shaders[name] = new ShaderViewModel { Path = file, Name = name };
                else
                    duplicates[name] = new ShaderViewModel { Path = file, Name = name };
            }

            foreach (var shader in shaders.Values)
            {
                if (EffectUtils.TryParseEffect(shader.Name, shaders, out var parsedShader))
                {
                    var baseShaderNames = parsedShader.BaseShaders.Select(s => s.ShaderClass.Name.Text).ToList();
                    System.Diagnostics.Debug.WriteLine(string.Join(", ", baseShaderNames));
                    if (baseShaderNames.Count > 0)
                    { 
                        var baseShaders = baseShaderNames
                            .Select(s => shaders.TryGetValue(s, out var b) ? b : null)
                            .Where(s => s != null);

                        foreach (var baseShader in baseShaders)
                        {
                            shader.BaseShaders.Add(baseShader);
                            baseShader.DerivedShaders.Add(shader);
                        }
                    }
                    else
                    {
                        yield return shader;
                    }
                }
                
            }

            Debug.WriteLine($"Found {shaders.Count} shaders");
        }
    }
}
