using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Avatars;
using EDIVE.Core;
using EDIVE.External.Promises;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.WordGenerating;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using Channel = FishNet.Transporting.Channel;
using EDIVE.Networking.Scenes;
using EDIVE.Networking.Utils;

namespace EDIVE.Networking.Players
{
    public class NetworkPlayerManager : ALoadableServiceBehaviour<NetworkPlayerManager>
    {
        [SerializeField]
        private NetworkPlayerController _PlayerPrefab;

        [ShowCreateNew]
        [SerializeField]
        private NetworkPlayerConfig _PlayerConfig;

        [ShowCreateNew]
        [SerializeField]
        private AvatarDefinition _DefaultAvatar;

        [ShowCreateNew]
        [SerializeField]
        private AWordGenerator _PlayerNameGenerator;
        
        public NetworkPlayerConfig PlayerConfig => _PlayerConfig;

        private NetworkManager _networkManager;

        public NetworkPlayerController LocalPlayer { get; private set; }
        public List<NetworkPlayerController> CurrentPlayers { get; } = new();

        private readonly List<(int id, Promise<NetworkPlayerController> promise)> _playerRequests = new();
        private Promise<NetworkPlayerController> _localPlayerRequest;
        
         public event Action<int> ServerPlayerJoined;

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = InstanceFinder.NetworkManager;
            if (_networkManager == null)
                return UniTask.CompletedTask;

            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
            _networkManager.ServerManager.OnRemoteConnectionState += OnServerRemoteConnectionState;
            _networkManager.ServerManager.RegisterBroadcast<PlayerCreationRequestMessage>(OnServerPlayerCreationRequest);
            return UniTask.CompletedTask;
        }

        public void RegisterPlayer(NetworkPlayerController player)
        {
            if (player.IsOwner)
            {
                LocalPlayer = player;
                _localPlayerRequest?.Dispatch(player);
                _localPlayerRequest = null;
            }
            
            if (CurrentPlayers.Contains(player))
                return;

            CurrentPlayers.Add(player);
            if (_playerRequests.TryGetFirst(p => p.id == player.OwnerId, out var request))
            {
                request.promise.Dispatch(player);
                _playerRequests.Remove(request);
            }
        }

        public void UnregisterPlayer(NetworkPlayerController player)
        {
            CurrentPlayers.Remove(player);
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_networkManager != null)
            {
                _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
                _networkManager.ServerManager.OnRemoteConnectionState -= OnServerRemoteConnectionState;
                _networkManager.ServerManager.UnregisterBroadcast<PlayerCreationRequestMessage>(OnServerPlayerCreationRequest);
            }
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

        public async UniTask<NetworkPlayerController> AwaitPlayerController(int clientID)
        {
            if (CurrentPlayers.TryGetFirst(c => c.OwnerId == clientID, out var playerController))
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
        private bool _sentCreateRequest;
        

        private void OnServerRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                if (CurrentPlayers.TryGetFirst(p => p.LocalConnection == conn, out var playerController))
                    CurrentPlayers.Remove(playerController);
            }
        }

        private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (asServer || _sentCreateRequest)
                return;

            _sentCreateRequest = true;
            var playerCreationRequest = new PlayerCreationRequestMessage();

            _networkManager.ClientManager.Broadcast(playerCreationRequest);
            DebugLite.Log("[NetworkPlayerManager] Sending request for player creation (after UserCenter profile load).");
        }

        private void OnServerPlayerCreationRequest(NetworkConnection conn, PlayerCreationRequestMessage request, Channel channel)
        {
            conn.WhenLoadedStartScenes(true, () =>
            {
                // Position will sync from players controls, so we can just instantiate player at origin
                var position = Vector3.zero;
                var rotation = Quaternion.identity;

                var netObj = _networkManager.GetPooledInstantiated(_PlayerPrefab.gameObject, position, rotation, true);
                var globalScene = AppCore.Services.Get<NetworkSceneManager>().GlobalScene;
                _networkManager.ServerManager.Spawn(netObj, conn, globalScene);
                _networkManager.SceneManager.AddOwnerToDefaultScene(netObj);

                var playerController = netObj.GetComponent<NetworkPlayerController>();
                CurrentPlayers.Add(playerController);

                ServerPlayerJoined?.Invoke(conn.ClientId);
                DebugLite.Log($"[NetworkPlayerManager] Instantiated a new player for ID:'{conn.ClientId}'");
            });
        }
    }
}
