using System.Reflection;
using System.Windows;

namespace XenkoShaderExplorer
{
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();
            InfoHeaderBlock.Text = "Shader Explorer for Xenko " + Assembly.GetEntryAssembly().GetName().Version;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e) => Close();
    }
}
