// Author: František Holubec
// Created: 26.03.2025

using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.Json;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Configuration
{
    public class LocalConfigSettings : ScriptableObject
    {
        [EnhancedTableList(ShowFoldout = false)]
        [SerializeField]
        private List<ConfigRecord> _Records = new();

        public async UniTask LoadConfigs()
        {
            var serializerSettings = new JsonSerializerSettings(ConfigUtility.SerializerSettings)
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var serializer = JsonSerializer.Create(serializerSettings);
            foreach (var record in _Records)
            {
                try
                {
                    await record.LoadConfig(serializer);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public async UniTask SaveConfigs(Predicate<ConfigRecord> recordFilter = null)
        {
            var serializerSettings = new JsonSerializerSettings(ConfigUtility.SerializerSettings)
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var serializer = JsonSerializer.Create(serializerSettings);
            foreach (var record in _Records)
            {
                if (recordFilter != null && !recordFilter(record))
                    continue;

                try
                {
                    await record.SaveConfig(serializer);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        [Serializable]
        public class ConfigRecord
        {
            [Required]
            [SerializeField]
            private string _FilePath;

            [Required]
            [SerializeField]
            [JsonAssetField("@ConfigUtility.Serializer")]
            private ScriptableObject _Asset;

            public string FilePath => _FilePath;
            public ScriptableObject Asset => _Asset;

            public async UniTask LoadConfig(JsonSerializer serializer)
            {
                var path = ConfigUtility.GetConfigPath(FilePath);
                if (!File.Exists(path))
                    return;

                try
                {
                    var json = await File.ReadAllTextAsync(path);
                    JsonAssetUtils.PopulateAsset(serializer, json, Asset);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            public async UniTask SaveConfig(JsonSerializer serializer)
            {
                var path = ConfigUtility.GetConfigPath(FilePath);
                try
                {
                    var json = JsonAssetUtils.SerializeAsset(serializer, Asset);
                    PathUtility.EnsurePathExists(path);
                    await File.WriteAllTextAsync(path, json.ToString(Formatting.Indented));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
