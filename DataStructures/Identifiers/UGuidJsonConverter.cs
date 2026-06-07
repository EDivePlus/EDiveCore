// Author: František Holubec
// Created: 07.06.2026

using System;
using Newtonsoft.Json;

namespace EDIVE.DataStructures.Identifiers
{
    public class UGuidJsonConverter : JsonConverter<UGuid>
    {
        public override void WriteJson(JsonWriter writer, UGuid value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override UGuid ReadJson(JsonReader reader, Type objectType, UGuid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return UGuid.Empty;

            var hexString = reader.Value?.ToString();
            return string.IsNullOrEmpty(hexString) ? UGuid.Empty : UGuid.Parse(hexString);
        }
    }
}
