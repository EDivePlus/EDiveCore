using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.External.Promises;
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
        }

        public void UnregisterPlayer(NetworkPlayerController player, bool asServer)
        {
            CurrentPlayers.Remove(player);
            if (!asServer && LocalPlayer == player)
                LocalPlayer = null;
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
    }
}
