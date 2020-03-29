using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace JupyterNetClient.Nbformat
{
    public class CellOutputConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(CellOutput);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var typeString = (string)jObject.Property("output_type");
            Enum.TryParse(typeString, out CellOutputType type);

            switch (type)
            {
                case CellOutputType.display_data:
                    return jObject.ToObject<DisplayDataCellOutput>();

                case CellOutputType.error:
                    return jObject.ToObject<ErrorCellOutput>();

                case CellOutputType.execute_result:
                    return jObject.ToObject<ExecuteResultCellOutput>();

                case CellOutputType.stream:
                    return jObject.ToObject<StreamOutputCellOutput>();

                default:
                    throw new Exception("Invalid output type");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            serializer.Serialize(writer, value);
    }
}
