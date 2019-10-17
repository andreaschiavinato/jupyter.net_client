using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace JupiterNetClient.Nbformat
{
    public class NbTextConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(string);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartArray:
                    var s = new StringBuilder();
                    string currLine = reader.ReadAsString();
                    var firstLoop = true;
                    while (currLine != null)
                    {
                        if (!firstLoop)
                            s.AppendLine();                        
                        firstLoop = false;
                        s.Append(currLine);
                        currLine = reader.ReadAsString();
                    }
                    return s.ToString();

                case JsonToken.String:
                    return reader.Value;

                default:
                    throw new Exception("");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var t = JToken.FromObject(((string)value).Split('\n'));
            t.WriteTo(writer);
        }
    }
}
