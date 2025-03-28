// Author: František Holubec
// Created: 26.03.2025

using System;
using System.Collections.Generic;
using System.IO;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Configuration
{
    public class LocalConfigSettings : ScriptableObject
    {
        [EnhancedTableList(ShowFoldout = false)]
        [SerializeField]
        private List<ConfigRecord> _Records = new();

        public void LoadConfigs()
        {
            var serializerSettings = new JsonSerializerSettings(UnityConverterInitializer.defaultUnityConvertersSettings)
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var serializer = JsonSerializer.Create(serializerSettings);
            foreach (var record in _Records)
            {
                var path = ConfigUtility.GetConfigPath(record.FilePath);
                if (!File.Exists(path))
                    continue;

                try
                {
                    var json = File.ReadAllText(path);
                    JsonAssetUtils.PopulateAsset(serializer, json, record.Asset);
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
        }
    }
}
