using System.Windows;

namespace JupiterNet.View
{
    /// <summary>
    /// Logica di interazione per InputBoxDlg.xaml
    /// </summary>
    public partial class InputBoxDlg : Window
    {

        public string Prompt { get; private set; }
        public string Value { get; set; }

        public InputBoxDlg(string prompt, bool password)
        {
            InitializeComponent();
            DataContext = this;
            Prompt = prompt;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
