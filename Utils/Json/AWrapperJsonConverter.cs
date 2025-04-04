using System;
using Newtonsoft.Json;

namespace EDIVE.Utils.Json
{
    public abstract class AWrapperJsonConverter<TWrapper, T> : JsonConverter<TWrapper> where TWrapper : class, new()
    {
        protected abstract TWrapper CreateWrapper(T inner);
        protected abstract T GetValue(TWrapper wrapper);
        protected abstract void SetValue(TWrapper wrapper, T value);
        
        public override TWrapper ReadJson(JsonReader reader, Type objectType, TWrapper existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
        
            var value = serializer.Deserialize<T>(reader);
            if (hasExistingValue && existingValue != null)
            {
                SetValue(existingValue, value);
                return existingValue;
            }
            return CreateWrapper(value);
        }

        public override void WriteJson(JsonWriter writer, TWrapper value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, GetValue(value));
        }
    }
}