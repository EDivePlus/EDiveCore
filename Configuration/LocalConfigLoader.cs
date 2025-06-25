// Author: František Holubec
// Created: 26.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;
using UnityEngine.Rendering;

namespace EDIVE.Configuration
{
    [Serializable]
    public class LocalConfigLoader : ILoadable
    {
        [SerializeField]
        [ShowCreateNew]
        private LocalConfigSettings _Settings;

        public UniTask Load(Action<float> progressCallback)
        {
            if (IsHeadless())
            {
                // Load existing settings
                _Settings.LoadConfigs();

                // Save the settings to file if they don't exist
                _Settings.SaveConfigs();
            }

            return UniTask.CompletedTask;
        }

        public static bool IsHeadless() =>
#if UNITY_SERVER && !UNITY_EDITOR
            true;
#else
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
#endif
    }
}
