using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace JupiterNetClient.Nbformat
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CellType
    {
        markdown,
        raw,
        code
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CellOutputType
    {
        stream,
        display_data,
        execute_result,
        error
    }

    public struct Metadata { }

    public abstract class CellBase
    {
        public CellType cell_type;

        [JsonConverter(typeof(NbTextConverter))]
        public string source;

        public Metadata metadata;

        [JsonIgnore]
        internal Notebook owner;

        [JsonIgnore]
        internal string msgId;

        public void UpdateValue(string source)
        {
            this.source = source;
            owner.UpdatedCell(this);
        }

        public string ToJson() =>
            JsonConvert.SerializeObject(this);

        internal void SetMsgId(string msg_id)
        {
            owner.Acquire();
            msgId = msg_id;
            owner.Release();
        }
    }

    public class MarkdownCell : CellBase
    {
        public MarkdownCell(Notebook owner, string source)
        {
            this.owner = owner;
            this.source = source;
            cell_type = CellType.markdown;
        }
        public MarkdownCell()
        {
            cell_type = CellType.markdown;
        }
    }

    public class RawCell : CellBase
    {
        public RawCell()
        {
            cell_type = CellType.raw;
        }
    }

    public class CodeCell : CellBase
    {
        public int? execution_count;

        [JsonProperty(ItemConverterType = typeof(CellOutputConverter))]
        public List<CellOutput> outputs;

        public CodeCell(Notebook owner, string source)
        {
            this.owner = owner;
            this.source = source;
            outputs = new List<CellOutput>();
            cell_type = CellType.code;
        }

        public CodeCell()
        {
            cell_type = CellType.code;
        }

        public CellOutput AddOutputFromMessage(JupyterMessage message)
        {
            var output = BuildOutputFromMessage(message);
            outputs.Add(output);
            owner.InsertedCellOutput(this, output);
            return output;
        }

        public void DeleteOutput(CellOutput output)
        {
            outputs.Remove(output);
            owner.DeletedCellOutput(this, output);
        }

        public void UpdateFromExecuteInputMessage(JupyterMessage message)
        {
            var content = (JupyterMessage.ExecuteInputContent)message.content;
            execution_count = content.execution_count;
            source = content.code;
            owner.UpdatedCell(this);
        }

        public void ClearOutputs()
        {
            outputs.Clear();
            owner.DeletedCellOutput(this, null);
        }

        private CellOutput BuildOutputFromMessage(JupyterMessage message)
        {
            switch (message.header.msg_type)
            {
                case JupyterMessage.Header.MsgType.execute_result:
                    return new ExecuteResultCellOutput((JupyterMessage.ExecuteResultContent)message.content);

                case JupyterMessage.Header.MsgType.display_data:
                    return new DisplayDataCellOutput((JupyterMessage.DisplayDataContent)message.content);

                case JupyterMessage.Header.MsgType.stream:
                    return new StreamOutputCellOutput((JupyterMessage.StreamContent)message.content);

                case JupyterMessage.Header.MsgType.error:
                    return new ErrorCellOutput((JupyterMessage.ErrorContent)message.content);

                default:
                    throw new Exception("BuildOutputFromMessage - Invalid message type");
            }
        }
    }

    public abstract class CellOutput
    {
        public CellOutputType output_type;
    }

    public class StreamOutputCellOutput : CellOutput
    {
        [JsonConverter(typeof(NbTextConverter))]
        public string text;
        public string name;

        public StreamOutputCellOutput(JupyterMessage.StreamContent content)
        {
            name = content.name;
            text = content.text;
            output_type = CellOutputType.stream;
        }

        public StreamOutputCellOutput()
        {
            output_type = CellOutputType.stream;
        }
    }

    public class DisplayDataCellOutput : CellOutput
    {
        public Metadata metadata;

        [JsonProperty(ItemConverterType =(typeof(NbTextConverter)))] //this is applied just to the values of the dictionary (not to the keys)
        public Dictionary<string, object> data;
        //public Dictionary<string, string> metdata;  --ignored for now

        public DisplayDataCellOutput(JupyterMessage.DisplayDataContent content)
        {
            data = content.data;
            output_type = CellOutputType.display_data;
        }

        public DisplayDataCellOutput()
        {
            output_type = CellOutputType.display_data;
        }
    }

    public class ExecuteResultCellOutput : CellOutput
    {
        public Metadata metadata;

        public int execution_count;

        [JsonProperty(ItemConverterType=(typeof(NbTextConverter)))] //this is applied just to the values of the dictionary (not to the keys)
        public Dictionary<string, string> data;

        public ExecuteResultCellOutput(JupyterMessage.ExecuteResultContent content)
        {
            execution_count = content.execution_count;
            data = content.data;
            output_type = CellOutputType.execute_result;
        }

        public ExecuteResultCellOutput()
        {
            output_type = CellOutputType.execute_result;
        }
    }

    public class ErrorCellOutput : CellOutput
    {
        public string ename;
        public string evalue;
        //public object traceback; --  ignored for now

        public ErrorCellOutput(JupyterMessage.ErrorContent content)
        {
            ename = content.ename;
            evalue = content.evalue;
            output_type = CellOutputType.error;
        }

        public ErrorCellOutput()
        {
            output_type = CellOutputType.error;
        }
    }
}
