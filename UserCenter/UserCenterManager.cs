// Author: František Holubec
// Created: 09.02.2026
// Updated: 18.02.2026 (savedata endpoints, removed attachments)

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.Networking.Players;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.UserCenter
{
    public class UserCenterManager : ALoadableServiceBehaviour<UserCenterManager>
    {
        [Header("Service API")]
        [SerializeField]
        private string _ServiceBaseUrl = "https://api.ediveplus.phil.muni.cz/service";
        
        [Header("Branch header (optional)")]
        [SerializeField]
        private string _BranchId = "-1";

        [Header("Profile key")]
        [SerializeField]
        private string _ProfileKey = "player_profile_v1";

        [Header("Timeouts")]
        [SerializeField, Min(1)]
        private int _ApiTimeoutSeconds = 5;

        [SerializeField, Min(1)]
        private int _AuthTimeoutSeconds = 5;

        public bool IsLoggedIn => AuthStorage.IsValid();

        public event Action<LoginResponse> OnLoginSucceeded;
        public event Action<long, string> OnLoginFailed;

        protected override UniTask LoadRoutine(Action<float> progressCallback) => UniTask.CompletedTask;

        private string TokenOrNull() => AuthStorage.GetAccessToken();

        private bool BranchEnabled =>
            !string.IsNullOrWhiteSpace(_BranchId) && _BranchId != "-1";

        private string AuthLoginUrl()
        {
            var baseu = (_ServiceBaseUrl ?? "").TrimEnd('/');
            return $"{baseu}/auth/login";
        }

        private string SavedataUrl(params string[] segments)
        {
            var baseu = (_ServiceBaseUrl ?? "").TrimEnd('/'); // => https://.../service
            var basePath = $"{baseu}/ediveplus/savedata"; // => https://.../service/ediveplus/savedata

            if (segments != null && segments.Length > 0)
                return basePath + "/" + string.Join("/", segments);

            return basePath;
        }

        private UnityWebRequest BuildJsonReq(
            string method,
            string url,
            object bodyOrNull,
            int timeoutSeconds,
            bool includeAuthHeader,
            bool includeBranchHeader
        )
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

            if (includeAuthHeader)
            {
                var token = TokenOrNull();
                if (!string.IsNullOrEmpty(token))
                    req.SetRequestHeader("Authorization", "Bearer " + token);
            }

            if (includeBranchHeader && BranchEnabled)
                req.SetRequestHeader("branch-id", _BranchId);

            req.timeout = Mathf.Max(10, timeoutSeconds);
            req.SetRequestHeader("Accept", "application/json");
            return req;
        }

        private CancellationToken DefaultCt(CancellationToken ct)
        {
            if (ct.CanBeCanceled) return ct;
            return this.GetCancellationTokenOnDestroy();
        }

        public void TryLoadStoredToken()
        {
            // storage is handled by AuthStorage
        }
        
        public void Login(string email, string password)
        {
            LoginAsync(email, password, this.GetCancellationTokenOnDestroy()).Forget();
        }

        [Button]
        public async UniTask<(bool ok, LoginResponse resp, long status, string message)> LoginAsync(
            string email,
            string password,
            CancellationToken ct = default
        )
        {
            ct = DefaultCt(ct);

            var url = AuthLoginUrl();
            var payload = new LoginRequest(email, password);

            using var req = BuildJsonReq(
                UnityWebRequest.kHttpVerbPOST,
                url,
                payload,
                timeoutSeconds: _AuthTimeoutSeconds,
                includeAuthHeader: false,
                includeBranchHeader: false
            );

            await req.SendWebRequest().ToUniTask(cancellationToken: ct);

            var raw = req.downloadHandler?.text ?? "";

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var jwt = ExtractJwtFromLoginResponse(raw);
                    if (string.IsNullOrEmpty(jwt))
                    {
                        var msg = "Invalid server response (token not found).";
                        OnLoginFailed?.Invoke(200, msg);
                        return (false, null, 200, msg);
                    }

                    var expUnix = JwtUtils.GetUnixExp(jwt);
                    var sub = JwtUtils.GetClaim(jwt, "sub");
                    var emailFromJwt = JwtUtils.GetClaim(jwt, "email");
                    var refreshFromJwt = JwtUtils.GetClaim(jwt, "refresh_token");

                    var userUuid = JwtUtils.GetClaim(jwt, "uuid") ?? JwtUtils.GetClaim(jwt, "userUuid");
                    var chosenUserId = !string.IsNullOrEmpty(userUuid)
                        ? userUuid
                        : (!string.IsNullOrEmpty(sub) ? sub : emailFromJwt);

                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var expiresIn = expUnix.HasValue ? (int)Mathf.Max(0, (int)(expUnix.Value - now)) : 0;

                    var resp = new LoginResponse
                    {
                        _AccessToken = jwt,
                        _RefreshToken = refreshFromJwt,
                        _UserId = chosenUserId,
                        _ExpiresIn = expiresIn
                    };

                    AuthStorage.Save(
                        accessToken: resp._AccessToken,
                        refreshToken: resp._RefreshToken,
                        userId: resp._UserId,
                        expUnixFromJwt: expUnix,
                        expiresInFromApi: resp._ExpiresIn
                    );

                    OnLoginSucceeded?.Invoke(resp);
                    return (true, resp, 200, "ok");
                }
                catch (Exception e)
                {
                    var msg = $"Login response parse error: {e.Message}";
                    OnLoginFailed?.Invoke(200, msg);
                    return (false, null, 200, msg);
                }
            }
            else
            {
                var status = req.responseCode;
                var msg = req.error;

                if (status == 401) msg = "Incorrect email or password.";
                else if (status == 403) msg = "Access denied.";
                else if (status >= 500) msg = "Server error. Please try again later.";
                else if (status == 0 && req.result == UnityWebRequest.Result.ConnectionError)
                    msg = "Unable to connect (network/TLS).";

                Debug.LogError($"[AUTH][LOGIN] ERR {status}: {msg}\n{raw}");
                OnLoginFailed?.Invoke(status, msg);
                return (false, null, status, msg);
            }
        }

        public void Logout() => AuthStorage.Clear();
        
        [Serializable]
        private class SavedataRecord
        {
            public long id;
            public string key;
            public string description;
            public long userId;
            public long branchId;
            public string userUuid;
            public UserBasicPojo userBasicPojo;
        }

        [Serializable]
        private class UserBasicPojo
        {
            public string uuid;
            public string firstName;
            public string surname;
            public string username;
            public string userType;
            public string email;
        }

        [Serializable]
        private class ContentWrapper<TItem>
        {
            public List<TItem> content;
        }

        private static List<SavedataRecord> TryParseSavedataList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            
            try
            {
                var direct = JsonConvert.DeserializeObject<List<SavedataRecord>>(raw);
                if (direct != null) return direct;
            }
            catch { /* ignore */ }

            try
            {
                var wrapper = JsonConvert.DeserializeObject<ContentWrapper<SavedataRecord>>(raw);
                return wrapper?.content;
            }
            catch { /* ignore */ }

            return null;
        } 

        public async UniTask<(bool ok, string msg)> LoadProfileFromSavedataAndApplyAsync(
            CancellationToken ct = default
        )
        {
            ct = DefaultCt(ct);

            if (!IsLoggedIn)
                return (false, "Not logged in.");

            if (!AppCore.Services.TryGet<NetworkPlayerManager>(out var npm) || npm == null)
                return (false, "NetworkPlayerManager not found.");

            // vezmeme list všech záznamů pro přihlášeného uživatele v branchi
            var listUrl = SavedataUrl() + "?pgSize=250";
            using (var req = BuildJsonReq(
                       UnityWebRequest.kHttpVerbGET,
                       listUrl,
                       bodyOrNull: null,
                       timeoutSeconds: _ApiTimeoutSeconds,
                       includeAuthHeader: true,
                       includeBranchHeader: true
                   ))
            {
                Debug.Log($"[PROFILE/SAVEDATA][LIST] GET {listUrl}");
                await req.SendWebRequest().ToUniTask(cancellationToken: ct);

                var text = req.downloadHandler?.text ?? "";

                if (req.responseCode == 404)
                {
                    Debug.Log("[PROFILE/SAVEDATA][LIST] 404 → žádný uložený profil; ponechávám výchozí hodnoty.");
                    return (true, "empty");
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[PROFILE/SAVEDATA][LIST] ERR {req.responseCode}: {req.error}\n{text}");
                    return (false, text);
                }

                var rows = TryParseSavedataList(text);
                if (rows == null || rows.Count == 0)
                {
                    Debug.Log("[PROFILE/SAVEDATA][LIST] prázdné – ponechávám výchozí.");
                    return (true, "empty");
                }

                var myUuid = AuthStorage.GetUserId();
                if (!string.IsNullOrEmpty(myUuid))
                    rows = rows.FindAll(r =>
                        string.Equals(r.userUuid, myUuid, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r.userBasicPojo?.uuid, myUuid, StringComparison.OrdinalIgnoreCase));

                var rec = rows.Find(r => string.Equals(r.key, _ProfileKey, StringComparison.OrdinalIgnoreCase));
                if (rec == null || string.IsNullOrEmpty(rec.description))
                {
                    Debug.Log("[PROFILE/SAVEDATA][LIST] nenalezen klíč – ponechávám výchozí.");
                    return (true, "empty");
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
                    return (false, "Invalid profile JSON in description.");

                // Apply to NetworkPlayerManager profile
                var prof = npm.PlayerProfile;

                if (!string.IsNullOrWhiteSpace(pj.username))
                    prof.username = pj.username;

                if (!string.IsNullOrWhiteSpace(pj.avatarId))
                {
                    // keep your logic: update last selected + persist avatarId
                    npm.OnLocalAvatarChanged(pj.avatarId);
                    prof.avatarId = pj.avatarId;
                }

                Debug.Log($"[PROFILE/SAVEDATA] Applied username='{prof.username}', avatarId='{prof.avatarId}'");
                return (true, "ok");
            }
        }

        public async UniTask<(bool ok, string msg)> SaveProfileToSavedataUpsertAsync(
            CancellationToken ct = default
        )
        {
            ct = DefaultCt(ct);

            if (!IsLoggedIn)
                return (false, "Not logged in.");

            if (!AppCore.Services.TryGet<NetworkPlayerManager>(out var npm) || npm == null)
                return (false, "NetworkPlayerManager not found.");
            
            var pj = new ProfileJson { username = npm.PlayerProfile.username, avatarId = npm.GetAvatarId() };
            var descriptionJson = JsonConvert.SerializeObject(pj);

            SavedataRecord existing = null;

            var listUrl = SavedataUrl() + "?pgSize=250";
            using (var sreq = BuildJsonReq(
                       UnityWebRequest.kHttpVerbGET,
                       listUrl,
                       bodyOrNull: null,
                       timeoutSeconds: _ApiTimeoutSeconds,
                       includeAuthHeader: true,
                       includeBranchHeader: true
                   ))
            {
                Debug.Log($"[PROFILE/SAVEDATA][LIST] GET {listUrl}");
                await sreq.SendWebRequest().ToUniTask(cancellationToken: ct);

                var stext = sreq.downloadHandler?.text ?? "";
                if (sreq.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(stext))
                {
                    var rows = TryParseSavedataList(stext);
                    if (rows != null)
                    {
                        var myUuid = AuthStorage.GetUserId();
                        if (!string.IsNullOrEmpty(myUuid))
                            rows = rows.FindAll(r =>
                                string.Equals(r.userUuid, myUuid, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(r.userBasicPojo?.uuid, myUuid, StringComparison.OrdinalIgnoreCase));

                        existing = rows.Find(r => string.Equals(r.key, _ProfileKey, StringComparison.OrdinalIgnoreCase));
                    }
                }
                else if (sreq.responseCode == 404)
                {
                    // treat as empty list
                }
                else if (sreq.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[PROFILE/SAVEDATA][LIST] ERR {sreq.responseCode}: {sreq.error}\n{stext}");
                    return (false, stext);
                }
            }

            if (existing != null && existing.id > 0)
            {
                // 2) UPDATE (PUT)
                var putUrl = SavedataUrl(existing.id.ToString());
                var body = new { key = _ProfileKey, description = descriptionJson };

                using (var preq = BuildJsonReq(
                           UnityWebRequest.kHttpVerbPUT,
                           putUrl,
                           body,
                           timeoutSeconds: _ApiTimeoutSeconds,
                           includeAuthHeader: true,
                           includeBranchHeader: true
                       ))
                {
                    Debug.Log($"[PROFILE/SAVEDATA][UPDATE] PUT {putUrl} body={JsonConvert.SerializeObject(body)}");
                    await preq.SendWebRequest().ToUniTask(cancellationToken: ct);

                    var ptext = preq.downloadHandler?.text ?? "";
                    if (preq.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[PROFILE/SAVEDATA][UPDATE] OK {preq.responseCode}: {ptext}");
                        return (true, ptext);
                    }

                    Debug.LogError($"[PROFILE/SAVEDATA][UPDATE] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                    return (false, ptext);
                }
            }
            else
            {
                // 3) CREATE (POST)
                var postUrl = SavedataUrl();
                var uuid = AuthStorage.GetUserId();

                object body = string.IsNullOrEmpty(uuid)
                    ? new { key = _ProfileKey, description = descriptionJson }
                    : new { key = _ProfileKey, description = descriptionJson, userUuid = uuid };

                using (var preq = BuildJsonReq(
                           UnityWebRequest.kHttpVerbPOST,
                           postUrl,
                           body,
                           timeoutSeconds: _ApiTimeoutSeconds,
                           includeAuthHeader: true,
                           includeBranchHeader: true
                       ))
                {
                    preq.SetRequestHeader("Accept", "application/json");

                    Debug.Log($"[PROFILE/SAVEDATA][CREATE] POST {postUrl} body={JsonConvert.SerializeObject(body)}");
                    await preq.SendWebRequest().ToUniTask(cancellationToken: ct);

                    var ptext = preq.downloadHandler?.text ?? "";
                    if (preq.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[PROFILE/SAVEDATA][CREATE] OK {preq.responseCode}: {ptext}");
                        return (true, ptext);
                    }

                    Debug.LogError($"[PROFILE/SAVEDATA][CREATE] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                    return (false, ptext);
                }
            }
        }

        public async UniTask<(bool ok, string msg)> UpdateProfileViaPutAsync(
            CancellationToken ct = default
        )
        {
            ct = DefaultCt(ct);

            if (!IsLoggedIn)
                return (false, "Not logged in.");

            if (!AppCore.Services.TryGet<NetworkPlayerManager>(out var npm) || npm == null)
                return (false, "NetworkPlayerManager not found.");

            // LIST → najdi key
            SavedataRecord existing = null;
            var listUrl = SavedataUrl() + "?pgSize=250";

            using (var sreq = BuildJsonReq(
                       UnityWebRequest.kHttpVerbGET,
                       listUrl,
                       bodyOrNull: null,
                       timeoutSeconds: _ApiTimeoutSeconds,
                       includeAuthHeader: true,
                       includeBranchHeader: true
                   ))
            {
                Debug.Log($"[PROFILE/SAVEDATA][LIST] GET {listUrl}");
                await sreq.SendWebRequest().ToUniTask(cancellationToken: ct);

                var stext = sreq.downloadHandler?.text ?? "";
                if (sreq.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(stext))
                {
                    var rows = TryParseSavedataList(stext);
                    if (rows != null)
                    {
                        var myUuid = AuthStorage.GetUserId();
                        if (!string.IsNullOrEmpty(myUuid))
                            rows = rows.FindAll(r =>
                                string.Equals(r.userUuid, myUuid, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(r.userBasicPojo?.uuid, myUuid, StringComparison.OrdinalIgnoreCase));

                        existing = rows.Find(r => string.Equals(r.key, _ProfileKey, StringComparison.OrdinalIgnoreCase));
                    }
                }
                else if (sreq.responseCode == 404)
                {
                    // treat as empty
                }
                else if (sreq.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[PROFILE/SAVEDATA][LIST] ERR {sreq.responseCode}: {sreq.error}\n{stext}");
                    return (false, stext);
                }
            }

            if (existing == null || existing.id == 0)
            {
                Debug.LogWarning("[PROFILE/SAVEDATA][PUT] Nenalezen existující profil — žádný update neproběhne.");
                return (false, "no existing");
            }

            var pj = new ProfileJson { username = npm.PlayerProfile.username, avatarId = npm.GetAvatarId() };
            var descriptionJson = JsonConvert.SerializeObject(pj);

            var putUrl = SavedataUrl(existing.id.ToString());
            var body2 = new { key = _ProfileKey, description = descriptionJson };

            using (var preq = BuildJsonReq(
                       UnityWebRequest.kHttpVerbPUT,
                       putUrl,
                       body2,
                       timeoutSeconds: _ApiTimeoutSeconds,
                       includeAuthHeader: true,
                       includeBranchHeader: true
                   ))
            {
                Debug.Log($"[PROFILE/SAVEDATA][PUT] {putUrl} body={JsonConvert.SerializeObject(body2)}");
                await preq.SendWebRequest().ToUniTask(cancellationToken: ct);

                var ptext = preq.downloadHandler?.text ?? "";
                if (preq.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[PROFILE/SAVEDATA][PUT] OK {preq.responseCode}: {ptext}");
                    return (true, ptext);
                }

                Debug.LogError($"[PROFILE/SAVEDATA][PUT] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                return (false, ptext);
            }
        }

        [Button("POST Save Profile (savedata upsert)")]
        [GUIColor(0.25f, 0.8f, 0.55f)]
        private async void Btn_SaveProfile_Upsert()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spusť Play Mode.");
                return;
            }

            var (ok, msg) = await SaveProfileToSavedataUpsertAsync();
            Debug.Log(ok ? "[PROFILE/SAVEDATA][UPSERT] OK" : "[PROFILE/SAVEDATA][UPSERT] ERR: " + msg);
        }

        [Button("GET Load Profile (from savedata)")]
        [GUIColor(0.4f, 0.7f, 1f)]
        private async void Btn_LoadProfile_FromSavedata()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spusť Play Mode.");
                return;
            }

            var (ok, msg) = await LoadProfileFromSavedataAndApplyAsync();
            Debug.Log(ok ? "[PROFILE/SAVEDATA][LOAD] OK" : "[PROFILE/SAVEDATA][LOAD] ERR: " + msg);
        }

        [Button("PUT Update Profile (savedata)")]
        [GUIColor(1f, 0.85f, 0.35f)]
        private async void Btn_UpdateProfile_Put()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spusť Play Mode.");
                return;
            }

            var (ok, msg) = await UpdateProfileViaPutAsync();
            Debug.Log(ok ? "[PROFILE/SAVEDATA][PUT] OK" : "[PROFILE/SAVEDATA][PUT] ERR: " + msg);
        }
        
        private static string ExtractJwtFromLoginResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            try
            {
                var jo = JToken.Parse(raw);
                return (string)(jo["token"] ?? jo["access_token"] ?? jo["accessToken"]);
            }
            catch
            {
                return null;
            }
        }
    }
}
