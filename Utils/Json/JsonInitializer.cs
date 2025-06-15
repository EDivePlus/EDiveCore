// Author: František Holubec
// Created: 15.06.2025

using EDIVE.Utils.Json.TypeNames;
using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Utils.Json
{
    public static class JsonInitializer
    {
        public static JsonSerializerSettings CustomJsonSerializerSettings { get; private set; }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        private static void Init()
        {
            UnityConverterInitializer.shouldAddConvertsToDefaultSettings = false;
            CustomJsonSerializerSettings = new JsonSerializerSettings(UnityConverterInitializer.defaultUnityConvertersSettings)
            {
                SerializationBinder = new JsonTypeNameSerializationBinder()
            };
            JsonConvert.DefaultSettings = GetCustomJsonSerializerSettings;
        }
        
        private static JsonSerializerSettings GetCustomJsonSerializerSettings() => CustomJsonSerializerSettings;
    }
}
