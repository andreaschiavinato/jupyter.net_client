using JupiterNetClient;
using JupiterNetClient.Nbformat;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace JupiterNet.ViewModel
{
    public class NotebookVM : ViewModelBase
    {
        public enum CellEveluationStatus
        {
            NotStarted,
            Running,
            Completed,
            Error
        }

        #region Cell definitions

        public abstract class CellVM : ViewModelBase
        {
            public CellBase AttachedCell;
            public CellOutput AttachedCellOutput;
            public virtual bool CanExecute => false;
        }

        public class InputCellVM : CellVM
        {
            public string Id { get; set; }
            public string Value { get; set; }
            public CellEveluationStatus Status { get; internal set; } = CellEveluationStatus.NotStarted;
            public override bool CanExecute => AttachedCell is CodeCell;
        }

        public class TextCellVM : CellVM
        {
            public string Value { get; set; }
            public bool IsError => AttachedCellOutput is ErrorCellOutput;
        }

        public class ImageCellVM : CellVM
        {
            public BitmapImage Value { get; set; }
        }
        #endregion
        
        public ObservableCollection<CellVM> Cells { get; set; }

        private readonly Dispatcher _dispatcher;

        public NotebookVM(Notebook notebookModel, Dispatcher dispatcher)
        {
            Cells = new ObservableCollection<CellVM>();
            _dispatcher = dispatcher;

            foreach (var cellModel in notebookModel.cells)
            {                
                var cellVm = BuildCell(cellModel);
                _dispatcher.Invoke(() => Cells.Add(cellVm));

                if (cellModel is CodeCell codeCell)
                {
                    var outputsVm = codeCell.outputs.Select(x => BuildOutputCellVm(codeCell, x)).ToList();
                    _dispatcher.Invoke(() => outputsVm.ForEach(Cells.Add));
                }
            }

            notebookModel.OnInsertedCell += InsertCell;
            notebookModel.OnUpdatedCell += UpdateCellVm;
            notebookModel.OnInsertedCellOutput += InsertCellOutput;
            notebookModel.OnDeletedCell += DeletedCell;
            notebookModel.OnDeletedCellOutput += DeletedCellOutput;
            notebookModel.OnMovedCell += MovedCell;
        }

        public void SetCellExecutionStarted(CodeCell cell)
        {
            var cellVm = Cells.First(item => item.AttachedCell == cell);
            if (cellVm is InputCellVM inputCellVM)
            {
                inputCellVM.Status = CellEveluationStatus.Running;
                inputCellVM.OnPropertyChanged(string.Empty);
            }
        }

        public void SetCellExecutionCompleted(CodeCell cell, JupyterMessage.ExecuteReplyContent content)
        {
            var cellVm = Cells.First(item => item.AttachedCell == cell);
            if (cellVm is InputCellVM inputCellVM)
            {
                inputCellVM.Status = content.status == "ok" ? CellEveluationStatus.Completed : CellEveluationStatus.Error;
                inputCellVM.OnPropertyChanged(string.Empty);
            }
        }

        private void InsertCell(object sender, CellBase cell)
        {
            var cellVm = BuildCell(cell);
            _dispatcher.Invoke(() => Cells.Add(cellVm));
        }

        private void UpdateCellVm(object sender, CellBase e)
        {
            var cellVm = Cells.First(item => item.AttachedCell == e);
            if (e is CodeCell codeCell && cellVm is InputCellVM inputCellVm)
            {
                inputCellVm.Id = $"[ {codeCell.execution_count} ]";
                inputCellVm.Value = codeCell.source;
                inputCellVm.OnPropertyChanged(string.Empty);
            }            
        }

        private void InsertCellOutput(object sender, (CodeCell cell, CellOutput output) e)
        {
            var cell = Cells.Last(item => item.AttachedCell == e.cell);
            var position = Cells.IndexOf(cell);
            var newCell = BuildOutputCellVm(e.cell, e.output);
            _dispatcher.Invoke(() => Cells.Insert(position + 1, newCell));
        }        

        private void DeletedCell(object sender, CellBase e)
        {
            //remove cell and all related output cells
            var itemsToRemove = Cells.Where(c => c.AttachedCell == e).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                Cells.Remove(itemToRemove);
            }
        }

        private void DeletedCellOutput(object sender, (CodeCell cell, CellOutput output) e)
        {
            //if outpout == null ==> deleted all outputs of cell
            if (e.output == null)
            {
                var itemsToRemove = Cells.Where(c => c.AttachedCell == e.cell && c.AttachedCellOutput != null).ToList();

                _dispatcher.Invoke(() =>
                {
                    foreach (var itemToRemove in itemsToRemove)
                    {
                        Cells.Remove(itemToRemove);
                    }
                });
            }
            else
            {
                Cells.Remove(Cells.First(c => c.AttachedCellOutput == e.output));
            }
        }

        private CellVM BuildCell(CellBase cell)
        {
            switch (cell)
            {
                case CodeCell codeCell:
                    return new InputCellVM
                    {
                        Id = codeCell.execution_count == null ? "" : $"[ {codeCell.execution_count} ]",
                        Value = codeCell.source,
                        AttachedCell = codeCell
                    };

                case MarkdownCell markdownCell:
                    return new InputCellVM
                    {
                        Value = markdownCell.source,
                        AttachedCell = markdownCell
                    };

                default:
                    throw new Exception("Invalid cell type");
            }
        }

        private CellVM BuildOutputCellVm(CodeCell cell, CellOutput output)
        {
            switch(output)
            {
                case ExecuteResultCellOutput outputc:
                    return new TextCellVM
                    {
                        Value = outputc.data[MimeTypes.TextPlain],
                        AttachedCell = cell,
                        AttachedCellOutput = output
                    };

                case DisplayDataCellOutput outputc:
                    return outputc.data.ContainsKey(MimeTypes.ImagePng)
                        ? (CellVM) new ImageCellVM
                        {
                            Value = Utils.Base64ToImage(outputc.data[MimeTypes.ImagePng].ToString()),
                            AttachedCell = cell,
                            AttachedCellOutput = output
                        }
                        : new TextCellVM
                        {
                            Value = outputc.data[MimeTypes.TextPlain].ToString(),
                            AttachedCell = cell,
                            AttachedCellOutput = output
                        };

                case StreamOutputCellOutput outputc:
                    return new TextCellVM
                    {
                        Value = outputc.text,
                        AttachedCell = cell,
                        AttachedCellOutput = output
                    };

                case ErrorCellOutput outputc:
                    return new TextCellVM
                    {
                        Value = $"{outputc.ename}: {outputc.evalue}",
                        AttachedCell = cell,
                        AttachedCellOutput = output
                    };

                default:
                    throw new Exception("Invalid output type");
            }
        }

        private void MovedCell(object sender, (int oldModelIndex, CellBase oldCell, int newModelIndex, CellBase newCell) e)
        {
            int oldVmIndex = -1;
            int oldVmLastIndex = -1;
            int newVmIndex = -1;
            int newVmLastIndex = -1;
            for (var i = 0; i < Cells.Count; i++)
            {
                var attachedCell = Cells[i].AttachedCell;
                if (attachedCell == e.oldCell)
                {
                    if (oldVmIndex == -1)
                        oldVmIndex = i;
                    oldVmLastIndex = i;
                }
                if (attachedCell == e.newCell)
                {
                    if (newVmIndex == -1)
                        newVmIndex = i;
                    newVmLastIndex = i;
                }
            }

            var moveCellDown = oldVmIndex < newVmIndex;
            var cellsToMove = oldVmLastIndex - oldVmIndex + 1;

            if (moveCellDown)
            {
                for (var i = 0; i < cellsToMove; i++)
                {
                    Cells.Move(oldVmIndex, newVmLastIndex);
                }
            }
            else
            {
                for (var i = 0; i < cellsToMove; i++)
                {
                    Cells.Move(oldVmIndex++, newVmIndex++);
                }
            }

        }
    }
} 
