// Author: František Holubec
// Created: 15.06.2025

using EDIVE.Utils.Json.TypeNames;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

#if JSON_UNITY_CONVERTERS
using Newtonsoft.Json.UnityConverters;
#endif

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
#if JSON_UNITY_CONVERTERS
            UnityConverterInitializer.shouldAddConvertsToDefaultSettings = false;
            CustomJsonSerializerSettings = new JsonSerializerSettings(UnityConverterInitializer.defaultUnityConvertersSettings);
#else
            CustomJsonSerializerSettings = new JsonSerializerSettings();
#endif
            CustomJsonSerializerSettings.SerializationBinder = new JsonTypeNameSerializationBinder();
            JsonConvert.DefaultSettings = GetCustomJsonSerializerSettings;
        }
        
        private static JsonSerializerSettings GetCustomJsonSerializerSettings() => CustomJsonSerializerSettings;
    }
}
