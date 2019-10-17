using System.Windows;

namespace JupiterNet.View
{
    /// <summary>
    /// Logica di interazione per About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
