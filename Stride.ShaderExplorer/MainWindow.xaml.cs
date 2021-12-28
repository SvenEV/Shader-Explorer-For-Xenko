using AurelienRibon.Ui.SyntaxHighlightBox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StrideShaderExplorer
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
            Title = "Shader Explorer for Stride " + Assembly.GetEntryAssembly().GetName().Version;
            codeView.CurrentHighlighter = HighlighterManager.Instance.Highlighters["SDSL"];
            StrideDirMode.ItemsSource = Enum.GetValues(typeof(StrideSourceDirMode)).Cast<StrideSourceDirMode>();
            StrideDirMode.SelectedIndex = 0;
            StrideDirMode.SelectionChanged += StrideDirMode_SelectionChanged;

            codeView.SelectionChanged += CodeView_SelectionChanged;
            BaseShaders.SelectionChanged += BaseShaders_SelectionChanged;
            MouseDown += MainWindow_MouseDown;
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1://Back button
                    Back();
                    break;
                case MouseButton.XButton2://forward button
                    Forward();
                    break;
                default:
                    break;
            }
        }

        private void BaseShaders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var s = BaseShaders.SelectedItem as string;
            if (s != null && ViewModel.ShaderMap.TryGetValue(s, out var shader))
            {
                SetShaderCode(shader);
                forwardStack.Push(s);
            }
        }

        private void SetShaderCode(ShaderViewModel shader, bool isNavigation = false)
        {
            if (shader != null)
            {
                codeView.Text = shader.Text;
                BaseShaders.ItemsSource = shader.BaseShaders.Select(s => s.Name);
                BaseShaders.SelectedItem = null;

                if (!isNavigation)
                {
                    backStack.Push(shader.Name);
                    BackButton.IsEnabled = backStack.Count > 1;
                    forwardStack.Clear();
                    ForwardButton.IsEnabled = false;
                }
            }

        }

        Stack<string> backStack = new Stack<string>();
        Stack<string> forwardStack = new Stack<string>();

        public void Back()
        {
            if (backStack.Count < 2)
                return;

            var s = backStack.Pop();
            var l = backStack.Peek();
            BackButton.IsEnabled = backStack.Count > 1;

            if (ViewModel.ShaderMap.TryGetValue(l, out var shader))
            {
                SetShaderCode(shader, isNavigation: true);
                forwardStack.Push(s);
                ForwardButton.IsEnabled = true;
            }
        }

        public void Forward()
        {
            if (forwardStack.Count < 1)
                return;

            var s = forwardStack.Pop();
            ForwardButton.IsEnabled = forwardStack.Count > 0;

            if (ViewModel.ShaderMap.TryGetValue(s, out var shader))
            {
                SetShaderCode(shader, isNavigation: true);
                backStack.Push(s);
                BackButton.IsEnabled = true;
            }
        }

        private void CodeView_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var length = codeView.Text.Length;

            if (length < 1)
                return;

            var start = codeView.CaretIndex;
            var end = codeView.CaretIndex;

            while (start >= 0 && start < length)
            {
                if (IsBorderChar(codeView.Text[start]))
                {
                    start++;
                    break;
                }

                start--;
            }

            while (end >= 0 && end < (length - 1))
            {
                if (IsBorderChar(codeView.Text[end]))
                    break;
                end++;
            }

            var word = codeView.Text.Substring(Math.Max(start, 0), Math.Max(end - start, 0));

            if (!string.IsNullOrWhiteSpace(word))
                ViewModel.SelectedWord = word;

            bool IsBorderChar(char c)
                => !(c == '_' || char.IsLetterOrDigit(c));
        }

        private void StrideDirMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ViewModel.StrideDirMode = (StrideSourceDirMode)StrideDirMode.SelectedIndex;
            ViewModel.Refresh();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SetShaderCode(e.NewValue as ShaderViewModel);
        }

        private void OnExpandAllButtonClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ExpandAll(true);
        }

        private void OnCollapseAllButtonClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ExpandAll(false);
        }

        private void OnInfoButtonClick(object sender, RoutedEventArgs e)
        {
            new InfoWindow().ShowDialog();
        }

        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            Back();
        }

        private void OnForwardButtonClick(object sender, RoutedEventArgs e)
        {
            Forward();
        }

        private void OnExploreButtonClick(object sender, RoutedEventArgs e)
        {
            if (backStack.Count > 0 && ViewModel.ShaderMap.TryGetValue(backStack.Peek(), out var shader))
            {
                // suppose that we have a test.txt at E:\
                string filePath = shader.Path;
                if (!File.Exists(filePath))
                {
                    return;
                }

                // combine the arguments together
                // it doesn't matter if there is a space after ','
                var argument = "/select, \"" + filePath + "\"";

                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
        }

        private void OnAddDirsButtonClick(object sender, RoutedEventArgs e)
        {
            var pw = new AdditionalPathsWindow();
            pw.DataContext = ViewModel;
            pw.ShowDialog();
            ViewModel.Refresh();
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.SaveUserSetting();
            base.OnClosed(e);
        }
    }
}
