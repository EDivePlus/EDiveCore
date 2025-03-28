// Author: František Holubec
// Created: 26.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

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
            _Settings.LoadConfigs();
            return UniTask.CompletedTask;
        }
    }
}
