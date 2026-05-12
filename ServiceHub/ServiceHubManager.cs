// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Auth;
using EDIVE.ServiceHub.RemoteContent;
using EDIVE.ServiceHub.SaveData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub
{
    public class ServiceHubManager : ALoadableServiceBehaviour<ServiceHubManager>
    {
        [SerializeField]
        [Required]
        private ServiceHubSettings _Settings;

        [SerializeField]
        [Required]
        [EnhancedBoxGroup("Modules", Color = "@ColorTools.Yellow", SpaceBefore = 8)]
        private ClientAuthService _ClientAuth;

        [SerializeField]
        [Required]
        [EnhancedBoxGroup("Modules")]
        private ServerAuthService _ServerAuth;

        [SerializeField]
        [Required]
        [EnhancedBoxGroup("Modules")]
        private SaveDataService _SaveData;

        [SerializeField]
        [Required]
        [EnhancedBoxGroup("Modules")]
        private RemoteContentApiService _RemoteContent;

        public ServiceHubSettings Settings => _Settings;
        public ClientAuthService ClientAuth => _ClientAuth;
        public ServerAuthService ServerAuth => _ServerAuth;
        public SaveDataService SaveData => _SaveData;
        public RemoteContentApiService RemoteContent => _RemoteContent;
        
        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            foreach (var module in GetAllModules())
                module.Initialize(_Settings);

            _ClientAuth.FlushBeforeLogoutAsync = _SaveData.FlushAllDirtyEntries;
            _ServerAuth.FlushBeforeLogoutAsync = _SaveData.FlushAllServerDirtyEntries;

            if (AuthStorage.Client.IsValid())
                await _ClientAuth.CheckClientAuthAsync(destroyCancellationToken);
        }

        private IEnumerable<IServiceHubModule> GetAllModules()
        {
            yield return _ClientAuth;
            yield return _ServerAuth;
            yield return _SaveData;
            yield return _RemoteContent;
        }
    }
}
