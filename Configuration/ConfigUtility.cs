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
        public static string GetConfigPath(string configName) => Path.Combine(ConfigFolderPath, $"{configName}.json");
        public static string ConfigFolderPath => PathUtility.GetRootAppDataPath("Configs");
        
        public static JsonSerializerSettings SerializerSettings => JsonInitializer.CustomJsonSerializerSettings;
        public static JsonSerializer Serializer { get; } = JsonSerializer.Create(SerializerSettings);
    }
}
