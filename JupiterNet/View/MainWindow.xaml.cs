using JupiterNet.ViewModel;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;

namespace JupiterNet.View
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewServices
    {
        public const string ApplicationTitle = "Jupyter.net";
        private int _completeCursorStart;
        private int _completeCursorEnd;

        public MainWindow()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)NotebookView.Items).CollectionChanged += OnNotebookViewCollectionChanged;
        }

        private void OnNotebookViewCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                NotebookView.ScrollIntoView(e.NewItems[0]);
            }
        }

        public string GetInput()
        {
            var result = string.Empty;
            Application.Current.Dispatcher.Invoke(() =>
            {                
                result = inputCommand.Text;
                inputCommand.Clear();
                NotebookView.IsEnabled = true;
            });
            return result;
        }

        public string PeekInput()
        {
            var result = string.Empty;
            Application.Current.Dispatcher.Invoke(() =>
            {
                result = inputCommand.Text;                              
            });
            return result;
        }
        public void SetInputText(string text) =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                inputCommand.Text = text;
                inputCommand.CaretIndex = text.Length;
                inputCommand.Focus();
            });

        public void BeginEditCell(string text) =>
            Application.Current.Dispatcher.Invoke(() =>
                {
                    NotebookView.IsEnabled = false;
                    inputCommand.Text = text;
                    inputCommand.CaretIndex = text.Length;
                    inputCommand.Focus();                    
                });

        public void CancelEditCell() => 
            Application.Current.Dispatcher.Invoke(() =>
                {
                    inputCommand.Clear();
                    NotebookView.IsEnabled = true;
                });

        public string SelectKernel(NotebookEditorVM viewModel) =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                var diag = new SelectKernelDlg();
                diag.DataContext = viewModel;
                diag.ShowDialog();
                return viewModel.SelectedKernel.Key;
            });

        public void ShowError(string message) => 
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        public string SelectPythonFile()
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Python files (*.py)|*.py|All files (*.*)|*.*"
            };
            return openFileDialog.ShowDialog()??false ? openFileDialog.FileName : string.Empty;
        }

        public string AskString(string prompt, bool password) =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                var diag = new InputBoxDlg(prompt, password);
                diag.ShowDialog();
                return diag.Value;
            });

        public string OpenFile()
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Notebook files (*.ipynb)|*.ipynb|All files (*.*)|*.*"
            };
            return openFileDialog.ShowDialog() ?? false ? openFileDialog.FileName : string.Empty;
        }

        public string AskFileName()
        {
            var saveFileDialog = new SaveFileDialog()
            {
                Filter = "Notebook files (*.ipynb)|*.ipynb|All files (*.*)|*.*"
            };
            return saveFileDialog.ShowDialog() ?? false ? saveFileDialog.FileName : string.Empty;
        }

        public DialogResult AskSaveDocument(string documentName)
        {
            var result = MessageBox.Show($"Save changes to {documentName}?", ApplicationTitle, MessageBoxButton.YesNoCancel);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    return ViewModel.DialogResult.Save;

                case MessageBoxResult.No:
                    return ViewModel.DialogResult.DontSave;

                case MessageBoxResult.Cancel:
                default:
                    return ViewModel.DialogResult.Cancel;
            }
        }

        public void SetCodeCompletion(List<string> matches, int cursorStart, int cursorEnd)
        {
            _completeCursorStart = cursorStart;
            _completeCursorEnd = cursorEnd;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var placementRect = inputCommand.GetRectFromCharacterIndex(inputCommand.CaretIndex, true);
                popComplete.PlacementTarget = inputCommand;
                popComplete.PlacementRectangle = placementRect;
                popComplete.IsOpen = true;
                lstComplete.SelectedIndex = 0;
                lstComplete.Focus();
            });
        }

        private void Ribbon_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) => 
            inputCommand.Focus();

        private void LstComplete_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                popComplete.IsOpen = false;
                CompleteCode(lstComplete.SelectedValue.ToString());
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                popComplete.IsOpen = false;
                inputCommand.Focus();
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }

        private void TextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            popComplete.IsOpen = false;
            CompleteCode(((FrameworkElement)e.OriginalSource).DataContext.ToString());
        }

        private void CompleteCode(string x)
        {
            inputCommand.Text = inputCommand.Text.Substring(0, _completeCursorStart) + x;
            inputCommand.CaretIndex = inputCommand.Text.Length;
            inputCommand.Focus();
        }

        public void ShowAbout()
        {
            var aboutWindow = new About();
            aboutWindow.ShowDialog();
        }
    }
}
