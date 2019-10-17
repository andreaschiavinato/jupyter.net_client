using System.Collections.Generic;

namespace JupiterNet.ViewModel
{
    public interface IViewServices
    {
        void ShowError(string v);
        string SelectKernel(NotebookEditorVM viewModel);
        string GetInput();
        string PeekInput();
        void SetInputText(string text);
        void BeginEditCell(string text);
        void CancelEditCell();
        string SelectPythonFile();
        string AskString(string prompt, bool password);
        string OpenFile();
        string AskFileName();
        DialogResult AskSaveDocument(string documentName);
        void SetCodeCompletion(List<string> matches, int cursor_start, int cursor_end);
        void ShowAbout();
    }
}
