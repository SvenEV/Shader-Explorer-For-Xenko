using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace StrideShaderExplorer
{
    public partial class AdditionalPathsWindow : Window
    {
        public AdditionalPathsWindow()
        {
            InitializeComponent();
        }

        private void OnPathBrowse(object sender, RoutedEventArgs e)
        {
            var mvm = (MainViewModel)DataContext;
            var item = (sender as Button)?.Tag as string;
            var index = mvm.AdditionalPaths.IndexOf(item);

            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (Directory.Exists(item))
                dialog.SelectedPath = item;

            dialog.Description = "Select shader folder";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                mvm.AdditionalPaths.Remove(item);
                mvm.AdditionalPaths.Insert(index, dialog.SelectedPath);

                if (item == "New path...")
                    mvm.AdditionalPaths.Add("New path...");
                Paths.Items.Refresh();
            }
        }

        private void OnRemove(object sender, RoutedEventArgs e)
        {
            var mvm = (MainViewModel)DataContext;

            var item = (sender as Button)?.Tag as string;

            if (item == "New path...")
                return;

            mvm.AdditionalPaths.Remove(item);
            Paths.Items.Refresh();
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e) => Close();
    }
}
