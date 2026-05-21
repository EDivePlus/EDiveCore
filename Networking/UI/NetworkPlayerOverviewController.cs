// Author: Michal Petr
// Created: 21.05.2026

using System.Collections.Generic;
using EDIVE.Core;
using EDIVE.NativeUtils;
using EDIVE.Networking.Players;
using UnityEngine;

namespace EDIVE.Networking.UI
{
    public class NetworkPlayerOverviewController : MonoBehaviour
    {
        [SerializeField]
        private NetworkPlayerDisplay _PlayerDisplayPrefab;
        
        [SerializeField]
        private Transform _PlayerDisplayContainer;
        
        private NetworkPlayerManager _networkPlayerManager;
        private readonly List<NetworkPlayerDisplay> _playerDisplays = new();

        private void Awake()
        {
            AppCore.Services.TryGet(out _networkPlayerManager);
        }

        private void OnEnable()
        {
            _networkPlayerManager.PlayerRegistered += OnPlayerRegistered;
            _networkPlayerManager.PlayerUnregistered += OnPlayerUnregistered;
            RefreshAllDisplays();
        }

        private void OnDisable()
        {
            _networkPlayerManager.PlayerRegistered -= OnPlayerRegistered;
            _networkPlayerManager.PlayerUnregistered -= OnPlayerUnregistered;
        }
        
        private void OnPlayerRegistered(NetworkPlayerController playerController)
        {
            AddPlayerDisplay(playerController);
        }
        
        private void OnPlayerUnregistered(NetworkPlayerController playerController)
        {
            RemovePlayerDisplay(playerController);
        }
        
        private void AddPlayerDisplay(NetworkPlayerController playerController)
        {
            var display = Instantiate(_PlayerDisplayPrefab, _PlayerDisplayContainer);
            display.SetPlayerController(playerController);
            _playerDisplays.Add(display);
        }
        
        private void RemovePlayerDisplay(NetworkPlayerController playerController)
        {
            var display = _playerDisplays.Find(d => d.PlayerController == playerController);
            if (display != null)
            {
                _playerDisplays.Remove(display);
                Destroy(display.gameObject);
            }
        }
        
        public void RefreshAllDisplays()
        {
            foreach (var display in _playerDisplays) 
            {
                Destroy(display.gameObject);
            }
            
            _playerDisplays.Clear();
            _PlayerDisplayContainer.DestroyChildren();
            
            foreach (var player in _networkPlayerManager.CurrentPlayers)
            {
                AddPlayerDisplay(player);
            }
        }
    }
}
