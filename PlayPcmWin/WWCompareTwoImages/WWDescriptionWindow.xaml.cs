using System;
using System.IO;
using System.Windows;

namespace WWCompareTwoImages
{
    public partial class WWDescriptionWindow : Window
    {
        public WWDescriptionWindow(Uri source)
        {
            InitializeComponent();
            mWebBrowser.Source = source;
        }

        public static Uri LocalPathToUri(string path)
        {
            var curDir = Directory.GetCurrentDirectory();
            return new Uri(String.Format("file:///{0}/{1}", curDir, path));
        }

        private void MButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
