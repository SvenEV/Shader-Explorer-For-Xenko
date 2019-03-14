using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace XenkoShaderExplorer
{
    public enum XenkoSourceDirMode
    {
        Official,
        Dev
    }

    public class MainViewModel : ViewModelBase
    {
        private const string XenkoEnvironmentVariable = "XenkoDir";
        private const string FallbackBasePath = @"C:\Program Files\Silicon Studio\Xenko\";

        private string _filterText;
        private IReadOnlyList<string> _path;

        /// <summary>
        /// The list of roots of the tree view. This includes all the shaders
        /// that do not inherit from any other shaders.
        /// </summary>
        public List<Shader> RootShaders { get; set; }

        /// <summary>
        /// The list of all shaders.
        /// </summary>
        public IEnumerable<Shader> AllShaders => ShadersInPostOrder();

        public string FilterText
        {
            get { return _filterText; }
            set
            {
                if (Set(ref _filterText, value))
                    UpdateFiltering();
            }
        }

        internal void Refresh()
        {
            try
            {
                List<string> basePath = null;
                switch (XenkoDirMode)
                {
                    case XenkoSourceDirMode.Official:
                        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        var nugetPackageDir = Path.Combine(userDir, ".nuget", "packages");
                        var directories = Directory.GetDirectories(nugetPackageDir) //package dir
                            .Where(dir => Path.GetFileName(dir).StartsWith("xenko", StringComparison.OrdinalIgnoreCase)) //xenko folders
                            .Select(dir => Directory.GetDirectories(dir).OrderBy(subdir => subdir, StringComparer.OrdinalIgnoreCase).LastOrDefault()) //latest version
                            .Where(dir => !dir.EndsWith("-dev")); //exclude local build package
                        basePath = directories.ToList();
                        break;
                    case XenkoSourceDirMode.Dev:
                        basePath = new List<string> { Environment.GetEnvironmentVariable(XenkoEnvironmentVariable) ?? FallbackBasePath };
                        //basePath = System.IO.Path.Combine(basePath, "sources", "engine", "Xenko.Engine", "Rendering");
                        break;
                    default:
                        break;
                }

                Paths = basePath;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Path to the Xenko installation folder.
        /// </summary>
        public IReadOnlyList<string> Paths
        {
            get { return _path; }
            set
            {
                if (Set(ref _path, value))
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

        public XenkoSourceDirMode XenkoDirMode { get; internal set; }

        public MainViewModel()
        {
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

        private IEnumerable<Shader> ShadersInPostOrder()
        {
            foreach (var rootShader in RootShaders)
                foreach (var shader in ShadersInPostOrder(rootShader))
                    yield return shader;
        }

        private static IEnumerable<Shader> ShadersInPostOrder(Shader shader)
        {
            foreach (var child in shader.DerivedShaders)
                foreach (var s in ShadersInPostOrder(child))
                    yield return s;
            yield return shader;
        }

        private IEnumerable<Shader> BuildShaderTree()
        {
            var files = Paths.SelectMany(path => Directory.GetFiles(path, "*.xksl", SearchOption.AllDirectories));
            var shaders = new Dictionary<string, Shader>();

            foreach (var file in files)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(file);
                if(!shaders.ContainsKey(name))
                    shaders[name] = new Shader { Path = file, Name = name };
            }

            foreach (var shader in shaders.Values)
            {
                var declaration = string.Join(" ", File.ReadLines(shader.Path)
                    .SkipWhile(s => !s.Trim().Contains("shader ") && !s.Trim().Contains("class ")) // From "shader" or "class"
                    .TakeWhile(s => !s.Contains("{"))); // To the bracket (exclusive)

                if (declaration != null)
                {
                    var colonIndex = declaration.IndexOf(':');

                    if (colonIndex != -1)
                    {
                        var baseShaderDeclaration = declaration.Substring(colonIndex + 1);
                        baseShaderDeclaration = Regex.Replace(baseShaderDeclaration, @"\<[\w\s\.\,]*\>", "");
                        baseShaderDeclaration = Regex.Replace(baseShaderDeclaration, "//.*", "");

                        var baseShaderNames = baseShaderDeclaration
                            .Split(',')
                            .Select(s => s.Trim());
                        System.Diagnostics.Debug.WriteLine(string.Join(", ", baseShaderNames));
                        if (!baseShaderNames.Contains("ShadowMapCasterBase"))
                        { //I have no clue why this shader doesn't exist. >w>

                            // There are shaders deriving from "ShadowMapCasterBase" which for some reason doesn't exit,
                            // so this base shader is filtered out via TryGetValue(...) == false.
                            var baseShaders = baseShaderNames
                                .Select(s => shaders.TryGetValue(s, out var b) ? b : null)
                                .Where(s => s != null);

                            foreach (var baseShader in baseShaders)
                            {
                                shader.BaseShaders.Add(baseShader);
                                baseShader.DerivedShaders.Add(shader);
                            }
                        }
                    }
                    else
                        yield return shader;
                }
            }

            Debug.WriteLine($"Found {shaders.Count} shaders");
        }
    }
}
