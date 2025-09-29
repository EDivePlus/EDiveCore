using System;
using System.IO;
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
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine.Networking;

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

        [Header("Profile POST config")]
        [SerializeField]
        private string _UploadUrl = "https://ediveplus.phil.muni.cz:8443/ediveplus/attachment";
        [SerializeField]
        private string _DefaultAttachmentType = "TABLEAUX";
        [SerializeField]
        private string _BranchId = "2";
        [SerializeField, Min(5)]
        private int _TimeoutSeconds = 60;

        [Serializable]
        private class AttachmentMeta
        {
            [JsonProperty("name")]
            public string Name;

            [JsonProperty("attachmentType")]
            public string AttachmentType;
        }
        
        private string _lastSelectedAvatarId;


        public NetworkPlayerConfig PlayerConfig => _PlayerConfig;

        private NetworkManager _networkManager;

        private PlayerProfile _playerProfile;
        public PlayerProfile PlayerProfile => _playerProfile ??= CreatePlayerProfile();

        public NetworkPlayerController LocalPlayer { get; private set; }
        private readonly List<NetworkPlayerController> _currentPlayers = new();
        private readonly List<(int id, Promise<NetworkPlayerController> promise)> _playerRequests = new();

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
                LocalPlayer = player;

            if (_currentPlayers.Contains(player))
                return;

            _currentPlayers.Add(player);
            if (_playerRequests.TryGetFirst(p => p.id == player.OwnerId, out var request))
            {
                request.promise.Dispatch(player);
                _playerRequests.Remove(request);
            }
        }

        public void UnregisterPlayer(NetworkPlayerController player) { _currentPlayers.Remove(player); }

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

        public async UniTask<NetworkPlayerController> AwaitPlayerController(int clientID)
        {
            if (_currentPlayers.TryGetFirst(c => c.OwnerId == clientID, out var playerController))
                return playerController;

            var promise = new Promise<NetworkPlayerController>();
            var record = (clientID, promise);
            _playerRequests.Add(record);

            var completionSource = new UniTaskCompletionSource<NetworkPlayerController>();
            promise.Then(r => completionSource.TrySetResult(r));

            // wait for player or timeout
            var timeout = UniTask.Delay(TimeSpan.FromSeconds(3));
            var result = await UniTask.WhenAny(completionSource.Task, timeout);
            _playerRequests.Remove(record);
            return result.result;
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
            // Only run on clients, we need player's profile for connection
            if (asServer)
                return;

            var playerCreationRequest = new PlayerCreationRequestMessage()
            {
                profile = PlayerProfile
            };
            _networkManager.ClientManager.Broadcast(playerCreationRequest);
            DebugLite.Log("[NetworkPlayerManager] Sending request for player creation.");
        }

        private void OnServerPlayerCreationRequest(NetworkConnection conn, PlayerCreationRequestMessage request, Channel channel)
        {
            // Position will sync from players controls, so we can just instantiate player at origin
            var position = Vector3.zero;
            var rotation = Quaternion.identity;

            var netObj = _networkManager.GetPooledInstantiated(_PlayerPrefab.gameObject, position, rotation, true);
            _networkManager.ServerManager.Spawn(netObj, conn, AppCore.Instance.RootScene);
            _networkManager.SceneManager.AddOwnerToDefaultScene(netObj);

            var playerController = netObj.GetComponent<NetworkPlayerController>();
            playerController.ApplyProfile(request.profile);
            _currentPlayers.Add(playerController);

            DebugLite.Log($"[NetworkPlayerManager] Instantiated a new player for ID:'{conn.ClientId}'");
        }

        private PlayerProfile CreatePlayerProfile()
        {
            if (_playerProfile != null)
                return _playerProfile;

            _playerProfile = new PlayerProfile()
            {
                username = GeneratePlayerName(),
                password = "",
                role = "guest",
                color = Color.HSVToRGB(Random.Range(0f, 1f), .75f, .75f),
                avatarId = _DefaultAvatar.UniqueID,
            };
            return _playerProfile;
        }
        

        public string GeneratePlayerName() { return _PlayerNameGenerator ? _PlayerNameGenerator.Generate() : $"Player_{Random.Range(1000, 9999)}"; }

        public void OnLocalAvatarChanged(string avatarId)
        {
            _lastSelectedAvatarId = avatarId;
            if (_playerProfile != null)
                _playerProfile.avatarId = avatarId;
        }

        public string GetAvatarId()
        {
            if (!string.IsNullOrEmpty(_lastSelectedAvatarId))
                return _lastSelectedAvatarId;
            if (LocalPlayer != null && !string.IsNullOrEmpty(LocalPlayer.AvatarID))
                return LocalPlayer.AvatarID;
            return PlayerProfile.avatarId;
        }
        
        private string BuildProfileJson()
        {
            var profile = PlayerProfile;
            var chosenAvatarId = GetAvatarId();

            string src =
                !string.IsNullOrEmpty(_lastSelectedAvatarId) ? "Cache" :
                (LocalPlayer != null && !string.IsNullOrEmpty(LocalPlayer.AvatarID)) ? "SyncVar" :
                "Profile.cache";

            var exportData = new
            {
                username = profile.username,
                avatarId = chosenAvatarId
            };

            Debug.Log($"[PROFILE POST] src={src}, LocalPlayer.AvatarID='{LocalPlayer?.AvatarID}', profile.avatarId='{profile.avatarId}', cache='{_lastSelectedAvatarId}'");
            Debug.Log($"[PROFILE POST][DEBUG] username = {exportData.username}, avatarId = {exportData.avatarId}");

            return JsonConvert.SerializeObject(exportData, Formatting.Indented);
        }

        [Button("POST Player Profile JSON")]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void PostProfileJson_Button() => PostProfileJson("Player profile (JSON)");

        public void PostProfileJson(string displayName = "player_profile.json")
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Musíš být v Play Mode, aby šel POST provést.");
                return;
            }

            StartCoroutine(PostProfileJson_Coroutine(displayName));
        }

        private System.Collections.IEnumerator PostProfileJson_Coroutine(string displayName)
        {
            // 1) JSON v paměti (ne na disk)
            string json = BuildProfileJson();
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(json);
            string fileName = "player_profile.json";

            // 2) metadata jako v Post_DataFile
            var meta = new AttachmentMeta
            {
                Name = string.IsNullOrWhiteSpace(displayName) ? fileName : displayName,
                AttachmentType = string.IsNullOrWhiteSpace(_DefaultAttachmentType) ? "TABLEAUX" : _DefaultAttachmentType
            };
            string metadataJson = JsonConvert.SerializeObject(meta);

            var formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", fileBytes, fileName, "application/json"),
                new MultipartFormDataSection("data", metadataJson, "application/json")
            };

            using (var request = UnityWebRequest.Post(_UploadUrl, formData))
            {
                var token = EDIVE.Networking.DatabaseManagement.AuthStorage.GetAccessToken();
                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                if (!string.IsNullOrEmpty(_BranchId))
                    request.SetRequestHeader("branch-id", _BranchId);

                request.timeout = Mathf.Max(10, _TimeoutSeconds);

                Debug.Log("[PROFILE POST] Odesílám player_profile.json na server...");
                yield return request.SendWebRequest();

                var body = request.downloadHandler != null ? request.downloadHandler.text : "";

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[PROFILE POST] Úspěch.");
                    if (!string.IsNullOrEmpty(body))
                        Debug.Log($"[PROFILE POST] Odpověď serveru: {body}");
                }
                else
                {
                    Debug.LogError($"[PROFILE POST] Chyba HTTP {request.responseCode}: {request.error}");
                    if (!string.IsNullOrEmpty(body))
                        Debug.LogError($"[PROFILE POST] Tělo odpovědi: {body}");
                }
            }
        }
    }
}
