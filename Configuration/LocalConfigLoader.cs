// Author: František Holubec
// Created: 26.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.Core.Services;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;
using UnityEngine.Rendering;

namespace EDIVE.Configuration
{
    [Serializable]
    public class LocalConfigLoader : IService, ILoadable
    {
        [SerializeField]
        [ShowCreateNew]
        private LocalConfigSettings _Settings;

        public async UniTask Load(Action<float> progressCallback)
        {
            if (IsStandalone())
            {
                // Load existing settings
                await _Settings.LoadConfigs();
            }
            // Save the settings to file if they don't exist
            await _Settings.SaveConfigs();

            AppCore.Services.Register(this);
        }
        
        public async UniTask SaveConfig(ScriptableObject config)
        {
            await _Settings.SaveConfigs(record => record.Asset == config);
        }
        
        public async UniTask SaveConfigs(Predicate<LocalConfigSettings.ConfigRecord> recordFilter = null)
        {
            await _Settings.SaveConfigs(recordFilter);
        }

        public static bool IsStandalone() =>
#if (UNITY_SERVER || UNITY_STANDALONE) && !UNITY_EDITOR
            true;
#else
            false;
#endif
    }
}
