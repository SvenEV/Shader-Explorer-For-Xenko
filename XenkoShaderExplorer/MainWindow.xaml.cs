using AurelienRibon.Ui.SyntaxHighlightBox;
using System.IO;
using System.Reflection;
using System.Windows;

namespace XenkoShaderExplorer
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
            this.Header.Text = "Shader Explorer for Xenko " + Assembly.GetEntryAssembly().GetName().Version;
            codeView.CurrentHighlighter = HighlighterManager.Instance.Highlighters["XKSL"];
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
