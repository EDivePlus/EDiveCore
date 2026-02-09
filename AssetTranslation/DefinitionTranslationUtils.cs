// Author: František Holubec
// Created: 14.04.2025


using UnityEngine;

#if FISHNET
using FishNet.Serializing;
#endif

namespace EDIVE.AssetTranslation
{
    public static class DefinitionTranslationUtils
    {
        public static bool TryGetDefinition<TDefinition>(string uniqueId, out TDefinition resultDefinition) where TDefinition : ScriptableObject, IUniqueDefinition
        {
            resultDefinition = null;
            if (string.IsNullOrEmpty(uniqueId))
                return false;

            if (!AssetTranslationConfig.Instance.TryGetTranslator(typeof(TDefinition), out var translator))
                return false;

            if (!translator.TryGet(uniqueId, out var definition) || definition is not TDefinition tDefinition)
                return false;

            resultDefinition = tDefinition;
            return true;
        }

        public static TDefinition GetDefinition<TDefinition>(string uniqueId) where TDefinition : ScriptableObject, IUniqueDefinition
        {
            return TryGetDefinition<TDefinition>(uniqueId, out var resultDefinition) ? resultDefinition : null;
        }
        
#if FISHNET
        public static void CustomWriteTranslatedDefinition<TDefinition>(this Writer writer, TDefinition value) where TDefinition : ScriptableObject, IUniqueDefinition
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

        public static TDefinition CustomReadTranslatedDefinition<TDefinition>(this Reader reader) where TDefinition : ScriptableObject, IUniqueDefinition
        {
            var uniqueId = reader.ReadStringAllocated();
            if (string.IsNullOrEmpty(uniqueId))
                return null;

            if (!AssetTranslationConfig.Instance.TryGetTranslator(typeof(TDefinition), out var translator))
                return null;

            return translator.TryGet(uniqueId, out var definition) && definition is TDefinition tDefinition ? tDefinition : null;
        }
#endif
    }
}
