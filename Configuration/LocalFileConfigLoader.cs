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
    public class LocalFileConfigLoader : ILoadable
    {
        [SerializeField]
        [ShowCreateNew]
        private LocalFileConfigDefinition _Definition;

        public UniTask Load(Action<float> progressCallback)
        {
            _Definition.LoadConfigs();
            return UniTask.CompletedTask;
        }
    }
}
