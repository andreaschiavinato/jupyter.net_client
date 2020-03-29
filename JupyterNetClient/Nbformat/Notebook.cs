using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace JupyterNetClient.Nbformat
{
    public static class MimeTypes
    {
        public const string TextPlain = "text/plain";
        public const string TextHtml = "text/html";
        public const string ImagePng = "image/png";
    }

    public class Notebook
    {
        #region Constants

        private const int def_nbformat = 4;
        private const int def_nbformat_minor = 2;

        #endregion

        #region Internal classes
        public struct Metadata
        {
            public KernelInfo kernel_info;
            public LanguageInfo language_info;
        }

        public struct KernelInfo
        {
            public string name;
        }

        public struct LanguageInfo
        {
            public string name;
            public string version;
        }
        #endregion

        #region Events

        public event EventHandler<CellBase> OnInsertedCell;
        public event EventHandler<CellBase> OnUpdatedCell;
        public event EventHandler<CellBase> OnDeletedCell;
        public event EventHandler<(CodeCell, CellOutput)> OnInsertedCellOutput;
        public event EventHandler<(CodeCell, CellOutput)> OnDeletedCellOutput;
        public event EventHandler<(int oldModelIndex, CellBase oldCell, int newModelIndex, CellBase newCell)> OnMovedCell;

        #endregion

        #region Public attrbutes (defined on https://nbformat.readthedocs.io/en/latest/format_description.html)

        public Metadata metadata;
        public int nbformat;
        public int nbformat_minor;
        
        [JsonProperty(ItemConverterType = typeof(CellConverter))]
        public List<CellBase> cells;

        #endregion

        #region Private attributes

        private bool _isDirty;
        private string _fileName;
        private Mutex _mut = new Mutex();

        #endregion

        #region Constructors

        public Notebook(KernelSpec kernelSpec, JupyterMessage.KernelInfoReplyContent.LanguageInfo languageInfo)
        {
            metadata.kernel_info.name = kernelSpec.spec.display_name;
            metadata.language_info.name = languageInfo.name;
            metadata.language_info.version = languageInfo.version;
            nbformat = def_nbformat;
            nbformat_minor = def_nbformat_minor;
            cells = new List<CellBase>();
            _isDirty = false;
        }

        [JsonConstructor()]
        protected Notebook()
        {
            _isDirty = false;
        }

        #endregion

        #region Methods

        public CodeCell AddCode(string source)
        {
            var cell = new CodeCell(this, source);
            cells.Add(cell);
            _isDirty = true;
            OnInsertedCell?.Invoke(this, cell);
            return cell;
        }

        public MarkdownCell AddMarkdown(string source)
        {
            var cell = new MarkdownCell(this, source);
            cells.Add(cell);
            _isDirty = true;
            OnInsertedCell?.Invoke(this, cell);            
            return cell;
        }

        public void AddCellFromJson(string cellJson)
        {
            var cell = JsonConvert.DeserializeObject<CellBase>(cellJson, new CellConverter());
            cells.Add(cell);
            cell.owner = this;
            OnInsertedCell?.Invoke(this, cell);
            _isDirty = true;

            if (cell is CodeCell codeCell)
            {
                codeCell.outputs.ForEach(output => OnInsertedCellOutput?.Invoke(this, (codeCell, output)));
            }
            
        }

        public void DeleteCell(CellBase cell)
        {
            cells.Remove(cell);
            _isDirty = true;
            OnDeletedCell?.Invoke(this, cell);
        }

        public static Notebook ReadFromFile(string fileName)
        {
            var result = JsonConvert.DeserializeObject<Notebook>(File.ReadAllText(fileName));
            result.FixCellOwners();
            result._fileName = fileName;
            return result;
        }

        public void Save(string fileName)
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(this));
            _fileName = fileName;
            _isDirty = false;
        }

        public bool IsDirty() => _isDirty;

        public string GetFileName() => _fileName;

        public string GetTitle() => string.IsNullOrEmpty(_fileName) ? "Untitled" : Path.GetFileName (_fileName);

        public bool HasFileName() => !string.IsNullOrEmpty(_fileName);

        public void UpdatedCell(CellBase cell)
        {
            OnUpdatedCell?.Invoke(this, cell);
            _isDirty = true;
        }

        public void InsertedCellOutput(CodeCell cell, CellOutput output)
        {
            OnInsertedCellOutput?.Invoke(this, (cell, output));
            _isDirty = true;
        }

        public void DeletedCellOutput(CodeCell cell, CellOutput output)
        {
            OnDeletedCellOutput?.Invoke(this, (cell, output));
            _isDirty = true;
        }

        public CodeCell FindParentCell(JupyterMessage message)
        {
            _mut.WaitOne();
            try
            {
                var parentMsgId = message.parent_header.msg_id;
                return (CodeCell)cells.Find(cell => cell.msgId == parentMsgId && cell is CodeCell);
            }
            finally
            {
                _mut.ReleaseMutex();
            }
        }

        public void MoveCellUp(CellBase cell) =>
            MoveCellIndex(cell, -1);

        public void MoveCellDown(CellBase cell) =>
            MoveCellIndex(cell, 1);

        public void MoveCellIndex(CellBase cell, int offset)
        {
            var idx = cells.IndexOf(cell);
            var destIdx = idx + offset;
            if (destIdx < 0 || destIdx >= cells.Count)
            {
                return;
            }

            var cellInDest = cells[destIdx];
            cells.RemoveAt(idx);
            cells.Insert(destIdx, cell);
            _isDirty = true;
            OnMovedCell?.Invoke(this, (idx, cell, destIdx, cellInDest));
        }

        public void Acquire() => _mut.WaitOne();
        public void Release() => _mut.ReleaseMutex();

        private void FixCellOwners()
        {
            foreach (var cell in cells)
                cell.owner = this;
        }

        #endregion
    }
}
