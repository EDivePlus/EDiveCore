// Author: František Holubec
// Created: 26.03.2025

using System.IO;
using EDIVE.NativeUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters;

namespace EDIVE.Configuration
{
    public static class ConfigUtility
    {
        public static string GetConfigPath(string configName) { return Path.Combine(ConfigFolderPath, $"{configName}.json"); }

        public static string ConfigFolderPath =>
#if UNITY_EDITOR
            PathUtility.GetAbsolutePath("Configs/");
#elif UNITY_STANDALONE
            Path.Combine(Application.dataPath, "Configs");
#else
            Path.Combine(Application.persistentDataPath, "Configs");
#endif

        public static JsonSerializerSettings SerializerSettings { get; } = new(UnityConverterInitializer.defaultUnityConvertersSettings);
        public static JsonSerializer Serializer { get; } = JsonSerializer.Create(SerializerSettings);
    }
}
