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
using System.Text;

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

        [Header("Profile persistence (savedata)")]
        [SerializeField]
        private string _SavedataBaseUrl = "https://ediveplus.phil.muni.cz:8443";
        [SerializeField]
        private string _SavedataContext = "ediveplus"; // => {base}/{context}/savedata
        [SerializeField]
        private string _SavedataBranchId = "2";
        [SerializeField, Min(5)]
        private int _TimeoutSeconds = 60;
        [SerializeField]
        private string _ProfileKey = "player_profile_v1";

        [Serializable]
        private class SavedataRecord
        {
            public long id;
            public string key;
            public string description;
            public long userId;
            public long branchId;
        }


        [Serializable]
        private class ProfileJson
        {
            public string username;
            public string avatarId;
        }

        [Serializable]
        private class ContentWrapper<TItem>
        {
            public List<TItem> content;
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
            if (asServer)
                return;

            // Nejprve načti profil ze savedata, pak teprve pošli PlayerCreationRequest.
            StartCoroutine(LoadProfileAndBroadcast());
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


        private System.Collections.IEnumerator LoadProfileAndBroadcast()
        {
            yield return LoadProfileFromSavedataAndApply((ok, _msg) => { });

            var playerCreationRequest = new PlayerCreationRequestMessage() {profile = PlayerProfile};
            _networkManager.ClientManager.Broadcast(playerCreationRequest);
            DebugLite.Log("[NetworkPlayerManager] Sending request for player creation (after savedata load).");
        }

        private string TokenOrNull() => EDIVE.Networking.DatabaseManagement.AuthStorage.GetAccessToken();

        private UnityWebRequest BuildJsonReq(string method, string url, object bodyOrNull)
        {
            var req = new UnityWebRequest(url, method);
            if (bodyOrNull != null)
            {
                var json = JsonConvert.SerializeObject(bodyOrNull);
                var bytes = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.SetRequestHeader("Content-Type", "application/json");
            }

            req.downloadHandler = new DownloadHandlerBuffer();

            var token = TokenOrNull();
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", "Bearer " + token);
            if (!string.IsNullOrEmpty(_SavedataBranchId))
                req.SetRequestHeader("branch-id", _SavedataBranchId);

            req.timeout = Mathf.Max(10, _TimeoutSeconds);
            return req;
        }

        private string SavedataUrl(params string[] segments)
        {
            var baseu = $"{_SavedataBaseUrl}/{_SavedataContext}/savedata";
            if (segments != null && segments.Length > 0)
                return baseu + "/" + string.Join("/", segments);
            return baseu;
        }


        public System.Collections.IEnumerator LoadProfileFromSavedataAndApply(Action<bool, string> onDone = null)
        {
            // vezmeme list všech záznamů pro přihlášeného uživatele v branchi
            var listUrl = SavedataUrl() + "?pgSize=250";
            using (var req = BuildJsonReq(UnityWebRequest.kHttpVerbGET, listUrl, null))
            {
                Debug.Log($"[PROFILE/SAVEDATA][LIST] GET {listUrl}");
                yield return req.SendWebRequest();

                var text = req.downloadHandler?.text ?? "";

                if (req.responseCode == 404)
                {
                    Debug.Log("[PROFILE/SAVEDATA][LIST] 404 → žádný uložený profil; ponechávám výchozí hodnoty.");
                    onDone?.Invoke(true, "empty");
                    yield break;
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[PROFILE/SAVEDATA][LIST] ERR {req.responseCode}: {req.error}\n{text}");
                    onDone?.Invoke(false, text);
                    yield break;
                }

                List<SavedataRecord> rows = null;

                // API může vracet buď prosté pole, nebo stránkované { content: [...] }
                try
                {
                    rows = JsonConvert.DeserializeObject<List<SavedataRecord>>(text);
                }
                catch
                {
                }

                if (rows == null)
                {
                    try
                    {
                        var wrapper = JsonConvert.DeserializeObject<ContentWrapper<SavedataRecord>>(text);
                        rows = wrapper?.content;
                    }
                    catch
                    {
                    }
                }

                if (rows == null || rows.Count == 0)
                {
                    Debug.Log("[PROFILE/SAVEDATA][LIST] prázdné – ponechávám výchozí.");
                    onDone?.Invoke(true, "empty");
                    yield break;
                }

                var rec = rows.Find(r => string.Equals(r.key, _ProfileKey, StringComparison.OrdinalIgnoreCase));
                if (rec == null || string.IsNullOrEmpty(rec.description))
                {
                    Debug.Log("[PROFILE/SAVEDATA][LIST] nenalezen klíč – ponechávám výchozí.");
                    onDone?.Invoke(true, "empty");
                    yield break;
                }

                ProfileJson pj = null;
                try
                {
                    pj = JsonConvert.DeserializeObject<ProfileJson>(rec.description);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PROFILE/SAVEDATA][PARSE] {e.Message}");
                }

                if (pj == null)
                {
                    onDone?.Invoke(false, "Invalid profile JSON in description.");
                    yield break;
                }

                var prof = PlayerProfile;
                if (!string.IsNullOrWhiteSpace(pj.username)) prof.username = pj.username;
                if (!string.IsNullOrWhiteSpace(pj.avatarId))
                {
                    _lastSelectedAvatarId = pj.avatarId;
                    prof.avatarId = pj.avatarId;
                }

                Debug.Log($"[PROFILE/SAVEDATA] Applied username='{prof.username}', avatarId='{prof.avatarId}'");
                onDone?.Invoke(true, "ok");
            }
        }

        public System.Collections.IEnumerator SaveProfileToSavedataUpsert(Action<bool, string> onDone = null)
        {
            // připrav JSON s profilem
            var pj = new ProfileJson {username = PlayerProfile.username, avatarId = GetAvatarId()};
            var descriptionJson = JsonConvert.SerializeObject(pj);

            // 1) LIST → najdi existující záznam podle _ProfileKey
            SavedataRecord existing = null;
            var listUrl = SavedataUrl() + "?pgSize=250";
            using (var sreq = BuildJsonReq(UnityWebRequest.kHttpVerbGET, listUrl, null))
            {
                Debug.Log($"[PROFILE/SAVEDATA][LIST] GET {listUrl}");
                yield return sreq.SendWebRequest();

                var stext = sreq.downloadHandler?.text ?? "";
                if (sreq.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(stext))
                {
                    List<SavedataRecord> rows = null;
                    try
                    {
                        rows = JsonConvert.DeserializeObject<List<SavedataRecord>>(stext);
                    }
                    catch
                    {
                    }

                    if (rows == null)
                    {
                        try
                        {
                            var wrapper = JsonConvert.DeserializeObject<ContentWrapper<SavedataRecord>>(stext);
                            rows = wrapper?.content;
                        }
                        catch
                        {
                        }
                    }

                    if (rows != null)
                        existing = rows.Find(r => string.Equals(r.key, _ProfileKey, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (existing != null && existing.id > 0)
            {
                // 2) UPDATE (PUT)
                var putUrl = SavedataUrl(existing.id.ToString());
                var body = new {key = _ProfileKey, description = descriptionJson};
                using (var preq = BuildJsonReq(UnityWebRequest.kHttpVerbPUT, putUrl, body))
                {
                    Debug.Log($"[PROFILE/SAVEDATA][UPDATE] PUT {putUrl} body={JsonConvert.SerializeObject(body)}");
                    yield return preq.SendWebRequest();

                    var ptext = preq.downloadHandler?.text ?? "";
                    if (preq.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[PROFILE/SAVEDATA][UPDATE] OK {preq.responseCode}: {ptext}");
                        onDone?.Invoke(true, ptext);
                    }
                    else
                    {
                        Debug.LogError($"[PROFILE/SAVEDATA][UPDATE] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                        onDone?.Invoke(false, ptext);
                    }
                }
            }
            else
            {
                // 3) CREATE (POST)
                var postUrl = SavedataUrl();
                var body = new {key = _ProfileKey, description = descriptionJson};
                using (var preq = BuildJsonReq(UnityWebRequest.kHttpVerbPOST, postUrl, body))
                {
                    Debug.Log($"[PROFILE/SAVEDATA][CREATE] POST {postUrl} body={JsonConvert.SerializeObject(body)}");
                    yield return preq.SendWebRequest();

                    var ptext = preq.downloadHandler?.text ?? "";
                    if (preq.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[PROFILE/SAVEDATA][CREATE] OK {preq.responseCode}: {ptext}");
                        onDone?.Invoke(true, ptext);
                    }
                    else
                    {
                        Debug.LogError($"[PROFILE/SAVEDATA][CREATE] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                        onDone?.Invoke(false, ptext);
                    }
                }
            }
        }


        [Button("POST Save Profile (savedata upsert)")]
        [GUIColor(0.25f, 0.8f, 0.55f)]
        private void Btn_SaveProfile_Upsert()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spusť Play Mode.");
                return;
            }

            StartCoroutine(SaveProfileToSavedataUpsert());
        }

        [Button("GET Load Profile (from savedata)")]
        [GUIColor(0.4f, 0.7f, 1f)]
        private void Btn_LoadProfile_FromSavedata()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spusť Play Mode.");
                return;
            }

            StartCoroutine(LoadProfileFromSavedataAndApply((ok, msg) =>
            {
                Debug.Log(ok
                    ? "[PROFILE/SAVEDATA][LOAD] OK"
                    : "[PROFILE/SAVEDATA][LOAD] ERR: " + msg);
            }));
        }

        [Button("PUT Update Profile (savedata)")]
        [GUIColor(1f, 0.85f, 0.35f)]
        private void Btn_UpdateProfile_Put()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spusť Play Mode.");
                return;
            }

            StartCoroutine(UpdateProfileViaPut());
        }

        private System.Collections.IEnumerator UpdateProfileViaPut(Action<bool, string> onDone = null)
        {
            // LIST → najdi key
            SavedataRecord existing = null;
            var listUrl = SavedataUrl() + "?pgSize=250";
            using (var sreq = BuildJsonReq(UnityWebRequest.kHttpVerbGET, listUrl, null))
            {
                Debug.Log($"[PROFILE/SAVEDATA][LIST] GET {listUrl}");
                yield return sreq.SendWebRequest();

                var stext = sreq.downloadHandler?.text ?? "";
                if (sreq.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(stext))
                {
                    List<SavedataRecord> rows = null;
                    try
                    {
                        rows = JsonConvert.DeserializeObject<List<SavedataRecord>>(stext);
                    }
                    catch
                    {
                    }

                    if (rows == null)
                    {
                        try
                        {
                            var wrapper = JsonConvert.DeserializeObject<ContentWrapper<SavedataRecord>>(stext);
                            rows = wrapper?.content;
                        }
                        catch
                        {
                        }
                    }

                    if (rows != null) existing = rows.Find(r => string.Equals(r.key, _ProfileKey, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (existing == null || existing.id == 0)
            {
                Debug.LogWarning("[PROFILE/SAVEDATA][PUT] Nenalezen existující profil — žádný update neproběhne.");
                onDone?.Invoke(false, "no existing");
                yield break;
            }

            var pj = new ProfileJson {username = PlayerProfile.username, avatarId = GetAvatarId()};
            var descriptionJson = JsonConvert.SerializeObject(pj);
            var putUrl = SavedataUrl(existing.id.ToString());
            var body = new {key = _ProfileKey, description = descriptionJson};

            using (var preq = BuildJsonReq(UnityWebRequest.kHttpVerbPUT, putUrl, body))
            {
                Debug.Log($"[PROFILE/SAVEDATA][PUT] {putUrl} body={JsonConvert.SerializeObject(body)}");
                yield return preq.SendWebRequest();

                var ptext = preq.downloadHandler?.text ?? "";
                if (preq.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[PROFILE/SAVEDATA][PUT] OK {preq.responseCode}: {ptext}");
                    onDone?.Invoke(true, ptext);
                }
                else
                {
                    Debug.LogError($"[PROFILE/SAVEDATA][PUT] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                    onDone?.Invoke(false, ptext);
                }
            }
        }
    }
}
