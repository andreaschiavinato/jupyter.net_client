using System.Windows;

namespace JupiterNet.View
{
    /// <summary>
    /// Logica di interazione per SelectKernelDlg.xaml
    /// </summary>
    public partial class SelectKernelDlg : Window
    {
        public SelectKernelDlg()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
