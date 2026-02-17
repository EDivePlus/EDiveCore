// Author: František Holubec
// Created: 09.02.2026

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;
using EDIVE.Networking.Players;

namespace EDIVE.UserCenter
{
    public class UserCenterManager : ALoadableServiceBehaviour<UserCenterManager>
    {
        [Header("Service API")]
        [SerializeField]
        private string _ServiceBaseUrl = "https://api.ediveplus.phil.muni.cz/service";
        
        [Header("Branch header (optional)")]
        [SerializeField]
        private string _BranchId = "-1"; // set to "2" if required; "-1" disables header

        [Header("Profile attachment")]
        [SerializeField]
        private string _ProfileKey = "player_profile_v1";

        [Header("Timeouts")]
        [SerializeField, Min(1)]
        private int _ApiTimeoutSeconds = 3;

        [SerializeField, Min(1)]
        private int _AuthTimeoutSeconds = 3;

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

        private string AttachmentsUrl(params string[] segments)
        {
            var baseu = (_ServiceBaseUrl ?? "").TrimEnd('/') + "/attachments";
            if (segments != null && segments.Length > 0)
                return baseu + "/" + string.Join("/", segments);
            return baseu;
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

        // Backward-compatible fire-and-forget API (same shape as AuthService).
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
            await req.SendWebRequest().WithCancellation(ct);

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
        
        [Button]
        public async UniTask<(bool ok, string msg, ProfileJson profile)> LoadProfileFromAttachmentsAsync(
            CancellationToken ct = default
        )
        {
            ct = DefaultCt(ct);

            // Spring Data REST pagination
            var listUrl = AttachmentsUrl() + "?page=0&size=200";

            using (var req = BuildJsonReq(
                       UnityWebRequest.kHttpVerbGET,
                       listUrl,
                       bodyOrNull: null,
                       timeoutSeconds: _ApiTimeoutSeconds,
                       includeAuthHeader: true,
                       includeBranchHeader: true
                   ))
            {
                Debug.Log($"[PROFILE/ATTACHMENTS][LIST] GET {listUrl}");
                await req.SendWebRequest().ToUniTask(cancellationToken: ct);

                var raw = req.downloadHandler?.text ?? "";

                if (req.responseCode == 404)
                    return (true, "empty", null);

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[PROFILE/ATTACHMENTS][LIST] ERR {req.responseCode}: {req.error}\n{raw}");
                    return (false, raw, null);
                }

                if (string.IsNullOrWhiteSpace(raw))
                    return (true, "empty", null);

                List<JToken> items;
                try
                {
                    items = ExtractHalCollection(raw, "attachments");
                }
                catch (Exception e)
                {
                    return (false, "Invalid attachments list JSON: " + e.Message, null);
                }

                if (items == null || items.Count == 0)
                    return (true, "empty", null);

                // Find by key in common fields
                var match = FindAttachmentByKey(items, _ProfileKey);
                if (match == null)
                    return (true, "empty", null);

                var json = ExtractProfileJsonFromAttachment(match);
                if (string.IsNullOrWhiteSpace(json))
                    return (false, "Profile attachment found, but no JSON payload fields found.", null);

                try
                {
                    var pj = JsonConvert.DeserializeObject<ProfileJson>(json);
                    if (pj == null) return (false, "Profile JSON is invalid.", null);
                    return (true, "ok", pj);
                }
                catch (Exception e)
                {
                    return (false, "Profile JSON parse error: " + e.Message, null);
                }
            }
        }

        [Button]
        public async UniTask<(bool ok, string msg)> SaveProfileToAttachmentsUpsertAsync(
            ProfileJson pj,
            CancellationToken ct = default
        )
        {
            ct = DefaultCt(ct);

            if (pj == null)
                return (false, "ProfileJson is null.");

            var profileJson = JsonConvert.SerializeObject(pj);
            var profileB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(profileJson));

            // 1) LIST
            var listUrl = AttachmentsUrl() + "?page=0&size=200";
            JToken existing = null;

            using (var lreq = BuildJsonReq(
                       UnityWebRequest.kHttpVerbGET,
                       listUrl,
                       bodyOrNull: null,
                       timeoutSeconds: _ApiTimeoutSeconds,
                       includeAuthHeader: true,
                       includeBranchHeader: true
                   ))
            {
                Debug.Log($"[PROFILE/ATTACHMENTS][LIST] GET {listUrl}");
                await lreq.SendWebRequest().ToUniTask(cancellationToken: ct);

                var raw = lreq.downloadHandler?.text ?? "";

                if (lreq.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(raw))
                {
                    try
                    {
                        var items = ExtractHalCollection(raw, "attachments");
                        existing = FindAttachmentByKey(items, _ProfileKey);
                    }
                    catch
                    {
                        // ignore, we'll create new
                    }
                }
            }

            // Payload tries to fit various common schemas (unknown fields may be ignored;
            // if server fails on unknown fields, remove the ones it complains about).
            var uuid = AuthStorage.GetUserId();
            var payload = new Dictionary<string, object>
            {
                ["name"] = _ProfileKey,
                ["key"] = _ProfileKey,
                ["fileName"] = _ProfileKey + ".json",
                ["filename"] = _ProfileKey + ".json",
                ["contentType"] = "application/json",
                ["mimeType"] = "application/json",

                // store as plain text
                ["description"] = profileJson,
                ["content"] = profileJson,
                ["text"] = profileJson,

                // store as base64
                ["data"] = profileB64
            };

            if (!string.IsNullOrWhiteSpace(uuid))
            {
                payload["userUuid"] = uuid;
            }

            // 2) UPDATE or CREATE
            if (existing != null)
            {
                var putUrl = GetSelfHref(existing) ?? BuildAttachmentUrlFromId(existing);
                if (string.IsNullOrWhiteSpace(putUrl))
                    return (false, "Attachment exists but has no self link/id.");

                using (var preq = BuildJsonReq(
                           UnityWebRequest.kHttpVerbPUT,
                           putUrl,
                           payload,
                           timeoutSeconds: _ApiTimeoutSeconds,
                           includeAuthHeader: true,
                           includeBranchHeader: true
                       ))
                {
                    Debug.Log($"[PROFILE/ATTACHMENTS][UPDATE] PUT {putUrl}");
                    await preq.SendWebRequest().ToUniTask(cancellationToken: ct);

                    var ptext = preq.downloadHandler?.text ?? "";
                    if (preq.result == UnityWebRequest.Result.Success)
                        return (true, ptext);

                    Debug.LogError($"[PROFILE/ATTACHMENTS][UPDATE] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                    return (false, ptext);
                }
            }
            else
            {
                var postUrl = AttachmentsUrl();

                using (var preq = BuildJsonReq(
                           UnityWebRequest.kHttpVerbPOST,
                           postUrl,
                           payload,
                           timeoutSeconds: _ApiTimeoutSeconds,
                           includeAuthHeader: true,
                           includeBranchHeader: true
                       ))
                {
                    Debug.Log($"[PROFILE/ATTACHMENTS][CREATE] POST {postUrl}");
                    await preq.SendWebRequest().ToUniTask(cancellationToken: ct);

                    var ptext = preq.downloadHandler?.text ?? "";
                    if (preq.result == UnityWebRequest.Result.Success)
                        return (true, ptext);

                    Debug.LogError($"[PROFILE/ATTACHMENTS][CREATE] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                    return (false, ptext);
                }
            }
        }
        

        [Button("GET Load Profile (attachments)")]
        [GUIColor(0.4f, 0.7f, 1f)]
        private async void Btn_LoadProfile_FromAttachments()
        {
            if (!Application.isPlaying) return;

            var (ok, msg, profile) = await LoadProfileFromAttachmentsAsync();
            Debug.Log(ok ? "[PROFILE/ATTACHMENTS][LOAD] OK" : "[PROFILE/ATTACHMENTS][LOAD] ERR: " + msg);

            if (ok && profile != null)
                Debug.Log($"[PROFILE/ATTACHMENTS] username='{profile.username}', avatarId='{profile.avatarId}'");
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

        private static List<JToken> ExtractHalCollection(string raw, string collectionName)
        {
            var root = JToken.Parse(raw);

            // plain array
            if (root.Type == JTokenType.Array)
                return new List<JToken>(root.Children());

            // Spring Data REST: { _embedded: { attachments: [...] } }
            var embedded = root["_embedded"];
            var arr = embedded?[collectionName];
            if (arr != null && arr.Type == JTokenType.Array)
                return new List<JToken>(arr.Children());

            // fallback: direct { attachments: [...] }
            var direct = root[collectionName];
            if (direct != null && direct.Type == JTokenType.Array)
                return new List<JToken>(direct.Children());

            return new List<JToken>();
        }

        private static JToken FindAttachmentByKey(List<JToken> items, string key)
        {
            if (items == null) return null;
            if (string.IsNullOrWhiteSpace(key)) return null;

            foreach (var it in items)
            {
                var name = GetString(it, "name", "key", "fileName", "filename", "title");
                if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                    return it;

                // sometimes fileName includes .json
                if (!string.IsNullOrWhiteSpace(name) &&
                    string.Equals(name.Trim(), key + ".json", StringComparison.OrdinalIgnoreCase))
                    return it;
            }

            return null;
        }

        private static string ExtractProfileJsonFromAttachment(JToken it)
        {
            // Prefer plain-text fields
            var json =
                GetString(it, "description") ??
                GetString(it, "content") ??
                GetString(it, "text");

            if (!string.IsNullOrWhiteSpace(json))
                return json;

            // Or base64 field "data"
            var dataB64 = GetString(it, "data");
            if (!string.IsNullOrWhiteSpace(dataB64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(dataB64);
                    return Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    // ignore
                }
            }

            return null;
        }

        private static string GetSelfHref(JToken it)
        {
            return (string)it?["_links"]?["self"]?["href"];
        }

        private string BuildAttachmentUrlFromId(JToken it)
        {
            var id = GetString(it, "id");
            if (string.IsNullOrWhiteSpace(id)) return null;
            return AttachmentsUrl(id);
        }

        private static string GetString(JToken it, params string[] keys)
        {
            if (it == null || keys == null) return null;
            foreach (var k in keys)
            {
                var v = it[k];
                if (v == null) continue;

                if (v.Type == JTokenType.String) return (string)v;
                // sometimes numbers (id) come as int/long
                if (v.Type == JTokenType.Integer || v.Type == JTokenType.Float) return v.ToString();
            }
            return null;
        }
    }

    [Serializable]
    public class ProfileJson
    {
        public string username;
        public string avatarId;
    }
}
