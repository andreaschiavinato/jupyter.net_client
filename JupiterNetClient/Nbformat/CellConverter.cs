using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JupiterNetClient.Nbformat
{
    public class CellConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(CellBase);
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var typeString = (string)jObject.Property("cell_type");
            Enum.TryParse(typeString, out CellType type);

            switch (type)
            {
                case CellType.markdown:
                    return jObject.ToObject<MarkdownCell>();

                case CellType.raw:
                    return jObject.ToObject<RawCell>();

                case CellType.code:
                    return jObject.ToObject<CodeCell>();

                default:
                    throw new Exception("Invalid cell type");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            serializer.Serialize(writer, value);
    }
}
