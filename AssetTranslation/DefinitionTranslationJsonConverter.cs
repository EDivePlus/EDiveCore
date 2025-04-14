// Author: František Holubec
// Created: 14.04.2025

using System;
using EDIVE.Utils.Json;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.AssetTranslation
{
    [Preserve]
    public class DefinitionTranslationJsonConverter : JsonConverter
    {
        public override bool CanRead => !UnityAssetConverterUtility.CheckDisabledAndRestore();
        public override bool CanWrite => !UnityAssetConverterUtility.CheckDisabledAndRestore();

        public override bool CanConvert(Type objectType)
        {
            return typeof(ScriptableObject).IsAssignableFrom(objectType) &&
                   typeof(IUniqueDefinition).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value is not IUniqueDefinition definition)
            {
                writer.WriteNull();
                return;
            }

#if UNITY_EDITOR || ASSET_TRANSLATION_LOGS
            if (AssetTranslationConfig.Instance.TryGetTranslator(value.GetType(), out var translator))
            {
                if (value is ScriptableObject so && !translator.Contains(so))
                {
                    Debug.LogError($"Definition with id '{definition.UniqueID}' is not registered in translator '{translator.name}'");
                }
            }
            else
            {
                Debug.LogError($"There is no translator for type '{value.GetType()}' or non is registered");
            }
#endif

            writer.WriteValue(definition.UniqueID);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var uniqueId = reader.Value as string;
            if (string.IsNullOrEmpty(uniqueId))
                return null;

            if (!AssetTranslationConfig.Instance.TryGetTranslator(objectType, out var translator))
                return null;

            return translator.TryGet(uniqueId, out var definition) ? definition : null;
        }
    }
}
