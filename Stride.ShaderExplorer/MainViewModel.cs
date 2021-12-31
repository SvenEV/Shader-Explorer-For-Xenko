using Microsoft.Toolkit.Mvvm.ComponentModel;
using Stride.Core.Shaders.Ast;
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

    public class MainViewModel : ObservableRecipient
    {
        private const string StrideEnvironmentVariable = "StrideDir";
        private const string NugetEnvironmentVariable = "NUGET_PACKAGES";

        private string _filterText;
        private bool _directParentsOnly = true;
        private ShaderViewModel _selectedShader;
        private IReadOnlyList<string> _paths;

        public List<string> AdditionalPaths
        {
            get;
            set;
        }

        public Dictionary<string, ShaderViewModel> shaders = new Dictionary<string, ShaderViewModel>();
        public Dictionary<string, Dictionary<ShaderViewModel, MemberList>> members = new Dictionary<string, Dictionary<ShaderViewModel, MemberList>>();
        public Dictionary<string, ShaderViewModel> ShaderMap => shaders;

        public bool FindMember(string name, ShaderViewModel shader, out MemberList mems, out List<ShaderViewModel> scopedShaders)
        {
            mems = null;
            scopedShaders = null;
            var result = members.TryGetValue(name, out var memberCandidates);

            if (result)
            {
                // defined locally?
                if (memberCandidates.TryGetValue(shader, out mems)) 
                {

                }

                //find base shaders that defines the member, could be multiple for method overrides
                scopedShaders = new List<ShaderViewModel>();
                var definingShader = shader;
                foreach (var baseShader in shader.BaseShaders)
                {

                    if (memberCandidates.TryGetValue(baseShader, out var ms))
                    {
                        //find highest definition in hierarchy
                        if (definingShader.BaseShaders.Contains(baseShader))
                        {
                            mems = ms;
                        }

                        scopedShaders.Add(baseShader);
                    }
                }

                //also add derived shaders
                foreach (var derivedShader in shader.DerivedShaders)
                {
                    if (memberCandidates.TryGetValue(derivedShader, out var _))
                    {
                        scopedShaders.Add(derivedShader);
                    }
                }
            }

            return result;
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
                if (SetProperty(ref _filterText, value))
                    UpdateFiltering();
            }
        }

        public bool DirectParentsOnly
        {
            get { return _directParentsOnly; }
            set
            {
                if (SetProperty(ref _directParentsOnly, value))
                    Refresh();
            }
        }

        public ShaderViewModel SelectedShader
        {
            get { return _selectedShader; }
            set
            {
                if (SetProperty(_selectedShader, value, v => _selectedShader = v))
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

                if (paths != null)
                    paths.AddRange(AdditionalPaths);
                else
                    paths = AdditionalPaths;

                Paths = paths;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal void SaveUserSetting()
        {
            Properties.UserSettings.Default.AdditionalPaths = string.Join(";", AdditionalPaths.Where(p => p != "New path..."));
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
                if (SetProperty(ref _paths, value))
                {
                    try
                    {
                        RootShaders = BuildShaderTree().OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
                        OnPropertyChanged(nameof(RootShaders));
                        OnPropertyChanged(nameof(AllShaders));
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
            AdditionalPaths.Add("New path...");
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

            shaders.Clear();
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
                    shader.ParsedShader = parsedShader;

                    // get all declrarations in this shader
                    foreach (var m in parsedShader.ShaderClass.Members.OfType<IDeclaration>() ?? Enumerable.Empty<IDeclaration>())
                    {
                        var mn = m.Name.Text;
                        if (string.IsNullOrWhiteSpace(mn))
                        {
                            continue;
                        }

                        if (!members.TryGetValue(mn, out var memberCandidates))
                        {
                            memberCandidates = new Dictionary<ShaderViewModel, MemberList>();
                        }

                        if (!memberCandidates.TryGetValue(shader, out var mems))
                        {
                            mems = new MemberList();
                        }

                        mems.Add(new MemberViewModel(mn, m));

                        memberCandidates[shader] = mems;
                        members[mn] = memberCandidates;
                    }

                    if (baseShaderNames.Count > 0)
                    { 
                        var baseShaders = baseShaderNames
                            .Select(s => shaders.TryGetValue(s, out var b) ? b : null)
                            .Where(s => s != null);

                        foreach (var baseShader in baseShaders)
                        {
                            shader.BaseShaders.Add(baseShader);
                            baseShader.DerivedShaders.Add(shader);
                            if (_directParentsOnly)
                            {
                                if (parsedShader.ShaderClass.BaseClasses.FirstOrDefault(bc => bc.Name.Text == baseShader.Name) != null)
                                {
                                    baseShader.TreeViewChildren.Add(shader);
                                }
                            }
                            else
                            {
                                baseShader.TreeViewChildren.Add(shader);
                            }
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
