// Author: František Holubec
// Created: 26.03.2025

using System.IO;
using EDIVE.NativeUtils;
using EDIVE.Utils.Json;
using Newtonsoft.Json;

namespace EDIVE.Configuration
{
    public static class ConfigUtility
    {
        public static string GetConfigPath(string configName) { return Path.Combine(ConfigFolderPath, $"{configName}.json"); }

        public static string ConfigFolderPath =>
#if UNITY_EDITOR
            PathUtility.GetAbsolutePath("Configs/");
#elif UNITY_STANDALONE
            Path.Combine(UnityEngine.Application.dataPath, "Configs");
#else
            Path.Combine(UnityEngine.Application.persistentDataPath, "Configs");
#endif

        public static JsonSerializerSettings SerializerSettings => JsonInitializer.CustomJsonSerializerSettings;
        public static JsonSerializer Serializer { get; } = JsonSerializer.Create(SerializerSettings);
    }
}
