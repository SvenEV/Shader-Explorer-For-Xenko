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
        }

        private void StrideDirMode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ViewModel.StrideDirMode = (StrideSourceDirMode)StrideDirMode.SelectedIndex;
            ViewModel.Refresh();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var shader = (Shader)e.NewValue;

            if (shader != null)
                codeView.Text = File.ReadAllText(shader.Path);
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
    }
}
