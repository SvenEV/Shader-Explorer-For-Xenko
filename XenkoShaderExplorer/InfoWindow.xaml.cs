using System.Windows;

namespace XenkoShaderExplorer
{
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e) => Close();
    }
}
