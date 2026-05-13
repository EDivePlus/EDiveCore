// Author: Michal Petr
// Created: 13.05.2026

using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UnityServices
{
    public class UnityRelayAllocator : MonoBehaviour
    {
        [SerializeField]
        private ServerConfig _Config;
        
        private Allocation _allocation;
        
        public async UniTask<Allocation> GetAllocationAsync()
        {
            if (_allocation != null)
                return _allocation;
            
            await Unity.Services.Core.UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            _allocation = await RelayService.Instance.CreateAllocationAsync(_Config.MaxPlayers);
            return _allocation;
        }
    }
}
