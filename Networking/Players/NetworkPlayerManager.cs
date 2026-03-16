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
using Random = UnityEngine.Random;
using Sirenix.OdinInspector;
using System.Linq;
using EDIVE.AssetTranslation;
using EDIVE.Networking.Scenes;
using EDIVE.Networking.Utils;
using UnityEngine.SceneManagement;
using EDIVE.UserCenter;
using System.Threading;

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
        
        [SerializeField, Min(0f)]
        private float _PlayerSummonRadius = 0.75f;
        
        public NetworkPlayerConfig PlayerConfig => _PlayerConfig;

        private NetworkManager _networkManager;

        private PlayerProfile _playerProfile;
        private string _lastSelectedAvatarId;
        
        // Todo get the player profile from UserCenter
        public PlayerProfile PlayerProfile => _playerProfile ??= CreatePlayerProfile();

        public NetworkPlayerController LocalPlayer { get; private set; }
        private readonly List<NetworkPlayerController> _currentPlayers = new();
        
        private readonly List<(int id, Promise<NetworkPlayerController> promise)> _playerRequests = new();
        private Promise<NetworkPlayerController> _localPlayerRequest;

        private readonly Dictionary<int, PlayerProfile> _playerProfiles = new();
        
        public event Action<int, PlayerProfile> ServerPlayerJoined;

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = InstanceFinder.NetworkManager;
            if (_networkManager == null)
                return UniTask.CompletedTask;

            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
            _networkManager.ServerManager.OnRemoteConnectionState += OnServerRemoteConnectionState;
            _networkManager.ServerManager.RegisterBroadcast<PlayerCreationRequestMessage>(OnServerPlayerCreationRequest);
            _networkManager.ServerManager.RegisterBroadcast<SummonPlayersToMeRequest>(OnServerSummonPlayersToMeRequest);
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
            
            if (_currentPlayers.Contains(player))
                return;

            _currentPlayers.Add(player);
            if (_playerRequests.TryGetFirst(p => p.id == player.OwnerId, out var request))
            {
                request.promise.Dispatch(player);
                _playerRequests.Remove(request);
            }
        }

        public void UnregisterPlayer(NetworkPlayerController player)
        {
            _currentPlayers.Remove(player);
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(MasterNetworkManager));
            dependencies.Add(typeof(UserCenterManager));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_networkManager != null)
            {
                _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
                _networkManager.ServerManager.OnRemoteConnectionState -= OnServerRemoteConnectionState;
                _networkManager.ServerManager.UnregisterBroadcast<PlayerCreationRequestMessage>(OnServerPlayerCreationRequest);
                _networkManager.ServerManager.UnregisterBroadcast<SummonPlayersToMeRequest>(OnServerSummonPlayersToMeRequest);
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
            if (_currentPlayers.TryGetFirst(c => c.OwnerId == clientID, out var playerController))
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

        private void ApplyProfileJson(PlayerProfileJson pj)
        {
            if (pj == null) return;

            if (!string.IsNullOrWhiteSpace(pj.Username))
                PlayerProfile.username = pj.Username;

            if (!string.IsNullOrWhiteSpace(pj.AvatarId))
                OnLocalAvatarChanged(pj.AvatarId);
        }

        private async UniTask PersistProfileJsonAsync(CancellationToken ct = default)
        {
            if (!AppCore.Services.TryGet<UserCenterManager>(out var uc) || uc == null)
                return;

            var pj = new PlayerProfileJson(PlayerProfile.username, GetAvatarId());

            await uc.SetPlayerProfileJson(pj, ct);
        }


        private void OnServerRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                if (_currentPlayers.TryGetFirst(p => p.LocalConnection == conn, out var playerController))
                    _currentPlayers.Remove(playerController);
            }
        }

        private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            if (asServer || _sentCreateRequest)
                return;

            _sentCreateRequest = true;

            UniTask.Void(async () =>
            {
                _playerProfile ??= CreatePlayerProfile();

                if (AppCore.Services.TryGet<UserCenterManager>(out var uc) && uc != null)
                {
                    var ct = this.GetCancellationTokenOnDestroy();
                    var res = await uc.GetPlayerProfileJson(ct);

                    if (res.IsOk)
                    {
                        ApplyProfileJson(res.Value);
                    }
                    else if (res.IsNotFound)
                    {
                        var pj = new PlayerProfileJson(PlayerProfile.username, GetAvatarId());
                        await uc.SetPlayerProfileJson(pj, ct);
                    }
                    else
                    {
                        Debug.LogWarning($"[NetworkPlayerManager] Profile load error: {res.ErrorMessage}");
                    }
                }

                var playerCreationRequest = new PlayerCreationRequestMessage()
                {
                    profile = PlayerProfile
                };

                _networkManager.ClientManager.Broadcast(playerCreationRequest);
                DebugLite.Log("[NetworkPlayerManager] Sending request for player creation (after UserCenter profile load).");
            });
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
                playerController.ApplyProfile(request.profile);
                _playerProfiles[conn.ClientId] = request.profile;
                _currentPlayers.Add(playerController);

                ServerPlayerJoined?.Invoke(conn.ClientId, request.profile);
                DebugLite.Log($"[NetworkPlayerManager] Instantiated a new player for ID:'{conn.ClientId}'");
            });
        }
        
        public bool TryGetPlayerProfile(int clientId, out PlayerProfile profile)
        {
            return _playerProfiles.TryGetValue(clientId, out profile);
        }

        private PlayerProfile CreatePlayerProfile()
        {
            if (_playerProfile != null)
                return _playerProfile;

            _playerProfile = new PlayerProfile
            {
                username = GeneratePlayerName(),
                password = "",
                role = "guest",
                color = Color.HSVToRGB(Random.Range(0f, 1f), .75f, .75f),
                avatar = _DefaultAvatar,
            };
            return _playerProfile;
        }

        public string GeneratePlayerName()
        {
            return _PlayerNameGenerator ? _PlayerNameGenerator.Generate() : $"Player_{Random.Range(1000, 9999)}";
        }
        public void OnLocalAvatarChanged(string avatarId)
        {
            _lastSelectedAvatarId = avatarId;
            PlayerProfile.avatarId = avatarId;
        }

        public string GetAvatarId()
        {
            if (!string.IsNullOrEmpty(_lastSelectedAvatarId))
                return _lastSelectedAvatarId;

            return PlayerProfile.avatarId;
        }
        
        public void UpdatePlayerProfile(PlayerProfile profile)
        {
            if (profile == null)
                return;

            PlayerProfile.username = profile.username;
            PlayerProfile.password = profile.password;
            PlayerProfile.role = profile.role;
            PlayerProfile.color = profile.color;
            
            // TODO sync with UserCenter
        }
        
        [Button]
        public void SummonPlayersToMe()
        {
            if (!Application.isPlaying)
                return;

            if (_networkManager == null || !_networkManager.IsClientStarted)
            {
                Debug.LogWarning("Client not connected.");
                return;
            }

            if (LocalPlayer == null)
            {
                Debug.LogWarning("LocalPlayer not ready");
                return;
            }

            var t = LocalPlayer.GetWorldPoseTransform();
            if (t == null)
            {
                Debug.LogWarning("LocalPlayer world pose transform not ready yet");
                return;
            }

            var sceneName = SceneManager.GetActiveScene().name;

            var msg = new SummonPlayersToMeRequest(
                sceneName,
                t.position,
                t.rotation,
                _PlayerSummonRadius
            );

            _networkManager.ClientManager.Broadcast(msg);
            Debug.Log($"[SUMMON] Request sent. scene='{sceneName}' pos={msg.Position}");
        }
        
        private void OnServerSummonPlayersToMeRequest(NetworkConnection sender, SummonPlayersToMeRequest request, Channel channel)
        {
            if (_networkManager == null || !_networkManager.IsServerStarted)
                return;

            if (!_currentPlayers.TryGetFirst(p => p != null && p.OwnerId == sender.ClientId, out var summoner))
            {
                Debug.LogWarning($"[SUMMON] No player controller for sender={sender.ClientId}. " +
                                 $"Players on server: {string.Join(", ", _currentPlayers.Select(p => p != null ? p.OwnerId.ToString() : "null"))}");
                return;
            }

            var sceneName = request.SceneName;

            var targets = _currentPlayers
                .Where(p => p != null &&
                            p.Owner != null &&
                            p.Owner != sender &&
                            InScene(p.Owner))
                .ToList();

            var count = targets.Count;
            for (var i = 0; i < count; i++)
            {
                var t = targets[i];
                var offset = ComputeCircleOffset(i, count, Mathf.Max(0f, request.Radius));
                
                t.ServerRequestTeleportOwner(request.Position + offset, request.Rotation);
            }

            Debug.Log($"[SUMMON] Requested teleport for {count} players in scene '{sceneName}' to sender {sender.ClientId}.");
            return;

            bool InScene(NetworkConnection c) =>
                c != null && c.Scenes != null && c.Scenes.Any(s => s.IsValid() && s.name == sceneName);
        }

        private static Vector3 ComputeCircleOffset(int index, int total, float radius)
        {
            if (total <= 1 || radius <= 0f) return Vector3.zero;
            var angle = (Mathf.PI * 2f) * (index / (float)total);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }
    }
}
