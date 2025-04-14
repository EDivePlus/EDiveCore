using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace EDIVE.Utils.Json
{
    [Preserve]
    public class DictionaryConverter<TKey,TValue> : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonReaderException($"Unexpected token {reader.TokenType} at the start of JSON.");

            var ret = existingValue as Dictionary<TKey, TValue> ?? new Dictionary<TKey, TValue>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                    {
                        var key = (TKey) reader.Value;
                        reader.Read();

                        if (ret.TryGetValue(key, out var value))
                        {
                            serializer.Populate(reader, value);
                            ret[key] = value;
                        }
                        else
                        {
                            value = serializer.Deserialize<TValue>(reader);
                            ret[key] = value;
                        }

                        break;
                    }
                    case JsonToken.EndObject: return ret;
                    default: throw new JsonReaderException($"Unexpected token {reader.TokenType} while reading JSON.");
                }
            }

            throw new JsonReaderException("Unexpected end of JSON.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
