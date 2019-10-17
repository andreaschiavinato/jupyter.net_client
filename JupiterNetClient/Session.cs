using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using ZeroMQ;

namespace JupiterNetClient
{
    public class Session
    {
        private const string _protocolVersion = "5.0";
        private Random _rnd;

        public string Id { get; }

        public string Key { get; set; }

        public string Username { get; }

        public Session(string username = "username")
        {
            _rnd = new Random();
            Id = NewId();
            Username = username;
        }

        //session.py msg function
        public JupyterMessage BuildMessage(JupyterMessage.Header.MsgType msgType, JupyterMessage.Content content)
        {
            var header = new JupyterMessage.Header()
            {
                msg_id = NewId(),
                username = Username,
                session = Id,
                date = DateTime.Now,
                msg_type = msgType,
                version = _protocolVersion
            };
            var result = new JupyterMessage()
            {
                header = header,
                parent_header = null,
                metadata = null,
                content = content
            };
            return result;
        }

        public ZMessage Serilize(JupyterMessage msg)
        {
            var content1 = JsonConvert.SerializeObject(msg.header);
            var content2 = "{}";
            var content3 = "{}";
            var content4 = JsonConvert.SerializeObject(msg.content);
            var allText = content1 + content2 + content3 + content4;
            var signature = HashEncode(HashHMAC(StringEncode(Key), StringEncode(allText)));

            var result = new ZMessage();
            result.Add(new ZFrame("<IDS|MSG>"));
            result.Add(new ZFrame(signature)); //HMAC-SHA256 signature
            result.Add(new ZFrame(content1));
            result.Add(new ZFrame(content2));
            result.Add(new ZFrame(content3));
            result.Add(new ZFrame(content4));
            return result;
        }

        private string NewId()
        {
            //id string (16 random bytes as hex-encoded text, chunks separated by '-')
            // example: f6e0d279-8e04cb8aed3ca5de943988f7
            var sb = new StringBuilder();
            for (var i = 0; i < 16; i++)
            {
                sb.Append(Convert.ToString(_rnd.Next(255), 16));
                if (i == 3)
                    sb.Append("-");
            }
            return sb.ToString();
        }

        private static byte[] HashHMAC(byte[] key, byte[] message)
        {
            var hash = new HMACSHA256(key);
            return hash.ComputeHash(message);
        }

        private static byte[] StringEncode(string text)
        {
            var encoding = new ASCIIEncoding();
            return encoding.GetBytes(text);
        }

        private static string HashEncode(byte[] hash) =>
            BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
