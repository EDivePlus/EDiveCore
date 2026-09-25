// Author: Michal Petr
// Created: 13.05.2026

#if UNITY_MULTIPLAYER && UNITY_TRANSPORT
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.UnityServices;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UnityServices
{
    public class UnityRelayAllocator : MonoBehaviour, IDependencyOwner
    {
        [SerializeField]
        private ServerConfig _Config;
        
        
        // New each time, allocation expires unused and is single session
        public async UniTask<Allocation> GetAllocationAsync()
        {
            return await RelayService.Instance.CreateAllocationAsync(_Config.MaxPlayers);
        }

        public IEnumerable<Type> GetDependencies()
        {
            yield return typeof(UnityServicesManager);
        }
    }
}
#endif
