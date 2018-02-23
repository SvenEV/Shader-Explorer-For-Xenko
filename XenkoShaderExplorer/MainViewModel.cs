﻿using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace XenkoShaderExplorer
{
    public class MainViewModel : ViewModelBase
    {
        private string _filterText;
		private string BASE_PATH = @"C:/Program Files/Silicon Studio/Xenko/GamePackages/";
		private string _path;

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

        /// <summary>
        /// Path to the Xenko installation folder.
        /// </summary>
        public string Path
        {
            get { return _path; }
            set
            {
                if (Set(ref _path, value))
                {
                    try
                    {
                        RootShaders = BuildShaderTree().ToList();
                        RaisePropertyChanged(nameof(RootShaders));
                        RaisePropertyChanged(nameof(AllShaders));
                        UpdateFiltering();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        public MainViewModel()
        {
            try
            {
				_path = Directory
					.EnumerateDirectories(BASE_PATH)
					.First(s => 
						System.IO.Path
							.GetFileName(s)
							.ToUpper()
							.StartsWith("XENKO")
					);
					
				RootShaders = BuildShaderTree().ToList();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
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
            var files = Directory.GetFiles(Path, "*.xksl", SearchOption.AllDirectories);
            var shaders = new Dictionary<string, Shader>();

            foreach (var file in files)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(file);
                shaders.Add(name, new Shader { Path = file, Name = name });
            }

            foreach (var shader in shaders.Values)
            {
				var stop = false;
				var text = string.Join(" ",
								File.ReadAllLines(shader.Path)
									.SkipWhile(s => !s.Trim().StartsWith("shader")) //From shader
									.TakeWhile(s => !s.Contains("{")) // To the bracket (exclusive)
						); 
						

                if (text != null)
                {
                    var colonIndex = text.IndexOf(':');

                    if (colonIndex != -1)
                    {
                        var baseShaderDeclaration = text.Substring(colonIndex + 1);
                        baseShaderDeclaration = Regex.Replace(baseShaderDeclaration, @"\<[\w\s\.\,]*\>", "");
                        baseShaderDeclaration = Regex.Replace(baseShaderDeclaration, "//.*", "");

                        var baseShaderNames = baseShaderDeclaration
                            .Split(',')
                            .Select(s => s.Trim());
						if (!baseShaderNames.Contains("ShadowMapCasterBase")) { //I have no clue why this shader doesn't exist. >w>

							try
							{
								foreach (var baseShader in baseShaderNames.Select(s => shaders[s]))
								{
									shader.BaseShaders.Add(baseShader);
									baseShader.DerivedShaders.Add(shader);
								}
							}
							catch (Exception e)
							{
								Console.Write(baseShaderNames);
							}
						}
                    }
                }
            }
			var x = 3;
            foreach (var rootShader in shaders.Values.Where(o => !o.BaseShaders.Any()))
            {
                yield return rootShader;
            }
        }
    }
}
