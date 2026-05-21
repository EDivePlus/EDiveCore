using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.External.Promises;
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

        private readonly List<(PlayerID id, Promise<NetworkPlayerController> promise)> _playerRequests = new();
        private Promise<NetworkPlayerController> _localPlayerRequest;
        
        public event Action<NetworkPlayerController> PlayerRegistered;
        public event Action<NetworkPlayerController> PlayerUnregistered;
        
        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            return UniTask.CompletedTask;
        }
        
        public void RegisterPlayer(NetworkPlayerController player, bool asServer)
        {
            player.gameObject.name = $"Player_{player.owner}";
            if (!asServer && player.isOwner)
            {
                player.gameObject.name += "_Local";
                LocalPlayer = player;
                _localPlayerRequest?.Dispatch(player);
                _localPlayerRequest = null;
            }

            if (CurrentPlayers.Contains(player))
                return;

            CurrentPlayers.Add(player);
            if (player.owner.HasValue &&
                _playerRequests.TryGetFirst(p => p.id == player.owner.Value, out var request))
            {
                request.promise.Dispatch(player);
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

        public async UniTask<NetworkPlayerController> AwaitLocalPlayerController()
        {
            if (LocalPlayer != null)
                return LocalPlayer;

            _localPlayerRequest ??= new Promise<NetworkPlayerController>();
            var completionSource = new UniTaskCompletionSource<NetworkPlayerController>();
            _localPlayerRequest.Then(r => completionSource.TrySetResult(r));
            return await completionSource.Task;
        }

        public async UniTask<NetworkPlayerController> AwaitPlayerController(PlayerID clientID)
        {
            if (CurrentPlayers.TryGetFirst(c => c.owner.HasValue && c.owner.Value == clientID, out var playerController))
                return playerController;

            var promise = new Promise<NetworkPlayerController>();
            var record = (clientID, promise);
            _playerRequests.Add(record);

            var completionSource = new UniTaskCompletionSource<NetworkPlayerController>();
            promise.Then(r => completionSource.TrySetResult(r));

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
