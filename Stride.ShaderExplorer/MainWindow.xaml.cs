using AurelienRibon.Ui.SyntaxHighlightBox;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

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
            var shader = (ShaderViewModel)e.NewValue;

            if (shader != null)
                codeView.Text = shader.Text;
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
