using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.Input.Controls;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.WordGenerating;
using PurrNet;
using UnityEngine;

namespace EDIVE.Networking.Players
{
    public class NetworkPlayerManager : ALoadableServiceBehaviour<NetworkPlayerManager>
    {
        [ShowCreateNew]
        [SerializeField]
        private AWordGenerator _PlayerNameGenerator;

        private NetworkManager _networkManager;

        public NetworkPlayerController LocalPlayer { get; private set; }
        public List<NetworkPlayerController> CurrentPlayers { get; } = new();

        private readonly List<(PlayerID id, UniTaskCompletionSource<NetworkPlayerController> completionSource)> _playerRequests = new();
        private UniTaskCompletionSource<NetworkPlayerController> _localPlayerRequest;
        
        public event Action<NetworkPlayerController> PlayerRegistered;
        public event Action<NetworkPlayerController> PlayerUnregistered;
        
        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            return UniTask.CompletedTask;
        }

        public string GeneratePlayerName()
        {
            return _PlayerNameGenerator != null ? _PlayerNameGenerator.Generate() : null;
        }
        
        public void RegisterPlayer(NetworkPlayerController player, bool asServer)
        {
            player.gameObject.name = $"Player_{player.owner}";
            if (!asServer && player.isOwner)
            {
                player.gameObject.name += "_Local";
                LocalPlayer = player;
                _localPlayerRequest?.TrySetResult(player);
                _localPlayerRequest = null;
            }

            if (CurrentPlayers.Contains(player))
                return;

            CurrentPlayers.Add(player);
            if (player.owner.HasValue &&
                _playerRequests.TryGetFirst(p => p.id == player.owner.Value, out var request))
            {
                request.completionSource.TrySetResult(player);
                _playerRequests.Remove(request);
            }
            
            PlayerRegistered?.Invoke(player);
        }

        public void UnregisterPlayer(NetworkPlayerController player, bool asServer)
        {
            CurrentPlayers.Remove(player);
            if (!asServer && LocalPlayer == player)
                LocalPlayer = null;
            
            PlayerUnregistered?.Invoke(player);
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
        }

        public UniTask<NetworkPlayerController> AwaitLocalPlayerController()
        {
            if (LocalPlayer != null)
                return UniTask.FromResult(LocalPlayer);

            _localPlayerRequest ??= new UniTaskCompletionSource<NetworkPlayerController>();
            return _localPlayerRequest.Task;
        }
        
        public bool TryGetPlayerController(PlayerID clientID, out NetworkPlayerController playerController)
        {
            if (CurrentPlayers.TryGetFirst(c => c.owner.HasValue && c.owner.Value == clientID, out var existingPlayerController))
            {
                playerController = existingPlayerController;
                return true;
            }

            playerController = null;
            return false;
        }

        public async UniTask<NetworkPlayerController> AwaitPlayerController(PlayerID clientID)
        {
            if (TryGetPlayerController(clientID, out var playerController))
                return playerController;

            var completionSource = new UniTaskCompletionSource<NetworkPlayerController>();
            var record = (clientID, completionSource);
            _playerRequests.Add(record);

            var timeout = UniTask.Delay(TimeSpan.FromMinutes(1));
            var result = await UniTask.WhenAny(completionSource.Task, timeout);
            _playerRequests.Remove(record);
            return result.result;
        }
        
        public bool CanKickPlayer(NetworkPlayerController player)
        {
            if (player == null || !player.owner.HasValue)
                return false;

            if (player == LocalPlayer)
                return false;

            var networkManager = NetworkManager.main;
            return networkManager != null && networkManager.isServer && networkManager.playerModule != null;
        }
        
        public bool TryKickPlayer(NetworkPlayerController player)
        {
            if (!CanKickPlayer(player))
                return false;

            NetworkManager.main.playerModule.KickPlayer(player.owner!.Value);
            return true;
        }
        
        public bool CanTeleportToPlayer(NetworkPlayerController player)
        {
            return player != null && player != LocalPlayer;
        }
        
        public void TeleportToPlayer(NetworkPlayerController player)
        {
            if (!CanTeleportToPlayer(player))
                return;

            if (!AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
                return;

            controlsManager.RequestTeleport(player.ControlsPosition, player.ControlsRotation);
        }
    }
}
