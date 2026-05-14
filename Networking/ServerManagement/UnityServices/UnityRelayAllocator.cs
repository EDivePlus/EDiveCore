// Author: Michal Petr
// Created: 13.05.2026

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
        
        private Allocation _allocation;
        
        public async UniTask<Allocation> GetAllocationAsync()
        {
            if (_allocation != null)
                return _allocation;
            
            _allocation = await RelayService.Instance.CreateAllocationAsync(_Config.MaxPlayers);
            return _allocation;
        }

        public IEnumerable<Type> GetDependencies()
        {
            yield return typeof(UnityServicesManager);
        }
    }
}
