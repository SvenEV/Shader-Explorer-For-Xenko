using System.Reflection;
using System.Windows;

namespace StrideShaderExplorer
{
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();
            InfoHeaderBlock.Text = "Shader Explorer for Stride " + Assembly.GetEntryAssembly().GetName().Version;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e) => Close();
    }
}
