// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Auth;
using EDIVE.ServiceHub.Lobby;
using EDIVE.ServiceHub.Probe;
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
        private RemoteContentService _RemoteContent;
        
        [SerializeField]
        [Required]
        [EnhancedBoxGroup("Modules")]
        private LobbyService _Lobby;

        [SerializeField]
        [Required]
        [EnhancedBoxGroup("Modules")]
        private ProbeService _Probe;

        public ServiceHubSettings Settings => _Settings;
        public ClientAuthService ClientAuth => _ClientAuth;
        public ServerAuthService ServerAuth => _ServerAuth;
        public SaveDataService SaveData => _SaveData;
        public RemoteContentService RemoteContent => _RemoteContent;
        public LobbyService Lobby => _Lobby;
        public ProbeService Probe => _Probe;
        
        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            foreach (var module in GetAllModules())
                module.Initialize(_Settings);

            _ClientAuth.OnLoggingOutAsync += _SaveData.FlushAllDirtyEntries;
            _ServerAuth.OnLoggingOutAsync += _SaveData.FlushAllServerDirtyEntries;

            if (AuthStorage.Client.IsValid())
                await _ClientAuth.CheckClientAuthAsync(destroyCancellationToken);
        }

        private IEnumerable<IServiceHubModule> GetAllModules()
        {
            yield return _ClientAuth;
            yield return _ServerAuth;
            yield return _SaveData;
            yield return _RemoteContent;
            yield return _Lobby;
            yield return _Probe;
        }
    }
}
