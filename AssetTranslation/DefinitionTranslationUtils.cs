// Author: František Holubec
// Created: 14.04.2025

using Mirror;
using UnityEngine;

namespace EDIVE.AssetTranslation
{
    public static class DefinitionTranslationUtils
    {
#if MIRROR
        public static void WriteDefinition<TDefinition>(this NetworkWriter writer, TDefinition value) where TDefinition : ScriptableObject, IUniqueDefinition
        {
#if UNITY_EDITOR || ASSET_TRANSLATION_LOGS
            if (AssetTranslationConfig.Instance.TryGetTranslator(value.GetType(), out var translator))
            {
                if (value is ScriptableObject so && !translator.Contains(so))
                {
                    Debug.LogError($"Definition with id '{value.UniqueID}' is not registered in translator '{translator.name}'");
                }
            }
            else
            {
                Debug.LogError($"There is no translator for type '{value.GetType()}' or non is registered");
            }
#endif
            writer.WriteString(value != null ? value.UniqueID : string.Empty);
        }

        public static TDefinition ReadDefinition<TDefinition>(this NetworkReader reader) where TDefinition : ScriptableObject, IUniqueDefinition
        {
            var uniqueId = reader.ReadString();
            if (string.IsNullOrEmpty(uniqueId))
                return null;

            if (!AssetTranslationConfig.Instance.TryGetTranslator(typeof(TDefinition), out var translator))
                return null;

            return translator.TryGet(uniqueId, out var definition) && definition is TDefinition tDefinition ? tDefinition : null;
        }
#endif
    }
}
