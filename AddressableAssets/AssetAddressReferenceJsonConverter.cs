using System;
using Newtonsoft.Json;

namespace EDIVE.AddressableAssets
{
    public class AssetAddressReferenceJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(AssetAddressReference).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is AssetAddressReference reference)
                writer.WriteValue(reference.Address);
            else
                writer.WriteNull();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String || reader.Value is not string address || string.IsNullOrEmpty(address))
                return null;
            
            return Activator.CreateInstance(objectType, address);
        }
    }
}