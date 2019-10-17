using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace JupiterNetClient
{
    // see https://jupyter-client.readthedocs.io/en/stable/messaging.html#general-message-format
    public struct JupyterMessage
    {
        #region Header class
        public class Header
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public enum MsgType
            {
                execute_request,
                execute_reply,
                kernel_info_request,
                kernel_info_reply,
                shutdown_request,
                shutdown_reply,
                interrupt_request,
                interrupt_reply,

                stream,
                status,
                display_data,
                update_display_data,
                execute_input,
                execute_result,
                error,
                clear_output,

                input_request,
                input_reply,
                inspect_request,
                inspect_reply,
                is_complete_request,
                is_complete_reply,
                complete_request,
                complete_reply,

                comm_open,
                comm_msg,
                comm_close
            }

            public string msg_id; //typically UUID, must be unique per message
            public string username;
            public string session; // typically UUID, should be unique per session
            public DateTime date; //ISO 8601 timestamp for when the message is created
            public MsgType msg_type;
            public string version;
        }
        #endregion

        #region Content classes
        public abstract class Content { };

        public class ExecuteRequestContent : Content
        {
            public string code;
            public bool silent;
            public bool store_history;
            public string user_expressions;
            public bool allow_stdin;
            public bool stop_on_error;
        }

        public class ExecuteReplyContent : Content
        {
            public string status;
            public int execution_count;
        }

        public class InspectRequestContent : Content
        {
            public string code;
            public int cursor_pos;
            public int detail_level;
        }

        public class InspectReplyContent : Content
        {
            public string status;
            public bool found;
            public Dictionary<string, string> data;
            public object metadata;
        }

        public class KernelInfoRequestContent : Content { }

        public class KernelInfoReplyContent : Content
        {
            public struct LanguageInfo
            {
                public string name;
                public string version;
                public string mimetype;
                public string file_extension;
                public string pygments_lexer;
                public object codemirror_mode;
                public string nbconvert_exporter;
            }

            public struct HelpLink
            {
                public string text;
                public string url;
            }

            public string status;
            public string protocol_version;
            public string implementation;
            public string implementation_version;
            public LanguageInfo language_info;
            public string banner;
            public List<HelpLink> help_links;
        }

        public class KernelInterruptRequestContent : Content { }

        public class KernelInterruptRequestReply : Content { }

        public class ExecuteInputContent : Content
        {
            public string code;
            public int execution_count;
        }

        public class ExecuteResultContent : Content
        {
            public int execution_count;
            public Dictionary<string, string> data;
            public object metadata;
        }

        public class StatusContent : Content
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public KernelState execution_state;
        }

        public class ErrorContent : Content
        {
            public string ename;
            public string evalue;
            public object traceback;
        }

        public class InputRequestContent : Content
        {
            public string prompt;
            public bool password;
        }

        public class InputReplyContent : Content
        {
            public string value;
        }

        public class StreamContent : Content
        {
            public string name;
            public string text;
        }

        public class DisplayDataContent : Content
        {
            public Dictionary<string, object> data;
            public object metadata;
        }

        public class ShutdownRequestContent : Content
        {
            public bool restart;
        }

        public class ShutdownReplyContent : Content
        {
            public bool restart;
        }

        public class IsCompleteRequest : Content
        {
            public string code;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum IsCompleteStatusEnum
        {
            complete, incomplete, invalid, unknown
        }

        public class IsCompleteReply : Content
        {
            public IsCompleteStatusEnum status;
            public string indent;
        }

        public class CompleteRequest : Content
        {
            public string code;
            public int cursor_pos;
        }

        public class CompleteReply : Content
        {
            public List<string> matches;
            public int cursor_start;
            public int cursor_end;
            public string status;
        }

        public class CommOpenContent : Content
        {
            public string comm_id;
            public string target_name;
            public Dictionary<string, object> data;
        }

        public class CommMsgContent : Content
        {
            public string comm_id;
            public Dictionary<string, object> data;
        }

        public class CommCloseContent : Content
        {
            public string comm_id;
            public Dictionary<string, object> data;
        }

        #endregion

        public Header header;
        public Header parent_header;
        public object metadata;
        public Content content;
    }
}
