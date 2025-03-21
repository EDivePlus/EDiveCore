using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EDIVE.Utils.SerializableDictionary
{
    public class SerializableDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (!objectType.IsGenericType) return false;
            return typeof(SerializableDictionary).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var args = objectType.GetGenericArguments();
            var dictResult = serializer.Deserialize(reader, typeof(IDictionary<,>).MakeGenericType(args));
            var overrideMethod = objectType.GetMethod("CopyValuesFrom");
            return overrideMethod?.Invoke(Activator.CreateInstance(objectType), new[] { dictResult });
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            serializer.Serialize(writer, ((SerializableDictionary) value).GetBackingDictionary());
        }
    }
}
