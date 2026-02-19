// Author: František Holubec
// Created: 09.02.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

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

        private readonly Dictionary<string, string> _memJsonCache = new(StringComparer.OrdinalIgnoreCase);

        private IJsonCodec _json;
        private ISavedataLocalStore _local;
        private UserCenterHttp _http;
        private SavedataAPI _savedata;
        
        private bool _inited;

        private void EnsureInit()
        {
            if (_inited) return;

            _json ??= new NewtonsoftJsonCodec();
            _local ??= new PlayerPrefsSavedataStore(prefix: "uc.savedata.");
            _http ??= new UserCenterHttp(TokenOrNull, BranchEnabled, () => _BranchId);
            _savedata ??= new SavedataAPI(_http, SavedataBaseUrl(), () => AuthStorage.GetUserId(), _ApiTimeoutSeconds);

            _inited = true;
        }

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            EnsureInit();
            return UniTask.CompletedTask;
        }

        private CancellationToken DefaultCt(CancellationToken ct)
        {
            EnsureInit();
            return ct.CanBeCanceled ? ct : this.GetCancellationTokenOnDestroy();
        }

        private bool BranchEnabled()
            => !string.IsNullOrWhiteSpace(_BranchId) && _BranchId != "-1";

        private string TokenOrNull()
            => AuthStorage.GetAccessToken();

        private string AuthLoginUrl()
        {
            var baseu = (_ServiceBaseUrl ?? "").TrimEnd('/');
            return $"{baseu}/auth/login";
        }

        private string SavedataBaseUrl()
        {
            var baseu = (_ServiceBaseUrl ?? "").TrimEnd('/');
            return $"{baseu}/ediveplus/savedata";
        }

        public void TryLoadStoredToken()
        {
        }

        public void Login(string email, string password) { LoginAsync(email, password, this.GetCancellationTokenOnDestroy()).Forget(); }

        public void Logout() => AuthStorage.Clear();
        
        
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
            var body = _json.Serialize(payload);

            var raw = await _http.SendRawAsync(
                method: UnityEngine.Networking.UnityWebRequest.kHttpVerbPOST,
                url: url,
                jsonBodyOrNull: body,
                includeAuthHeader: false,
                includeBranchHeader: false,
                timeoutSeconds: _AuthTimeoutSeconds,
                ct: ct
            );

            if (raw.Success)
            {
                try
                {
                    var jwt = ExtractJwtFromLoginResponse(raw.Raw);
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
                    var expiresIn = expUnix.HasValue ? Mathf.Max(0, (int) (expUnix.Value - now)) : 0;

                    var resp = new LoginResponse
                    {
                        _AccessToken = jwt,
                        _RefreshToken = refreshFromJwt,
                        _UserId = chosenUserId,
                        _ExpiresIn = expiresIn
                    };

                    AuthStorage.Save(resp._AccessToken, resp._RefreshToken, resp._UserId, expUnix, resp._ExpiresIn);

                    // když login proběhne, server index může být jiný uživatel → invalidate
                    _savedata?.InvalidateIndex();

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

            var status = raw.StatusCode;
            var msg2 = raw.Error;

            if (status == 401) msg2 = "Incorrect email or password.";
            else if (status == 403) msg2 = "Access denied.";
            else if (status >= 500) msg2 = "Server error. Please try again later.";
            else if (status == 0) msg2 = "Unable to connect (network/TLS/offline).";

            OnLoginFailed?.Invoke(status, msg2);
            return (false, null, status, msg2);
        }
        
        public async UniTask<DataResult<T>> GetData<T>(string key, CancellationToken ct = default, bool forceRefresh = false)
        {
            ct = DefaultCt(ct);
            
            if (IsLoggedIn && _savedata != null)
            {
                var server = await _savedata.GetDescriptionJsonByKeyAsync(key, ct, forceRefresh);

                if (server.Success)
                {
                    if (_json.TryDeserialize<T>(server.Result, out var obj, out var derr))
                    {
                        _local.Set(key, server.Result);
                        return DataResult<T>.Ok(obj, fromServer: true, fromLocal: false, fromMemory: false);
                    }

                    return DataResult<T>.Error($"Savedata JSON parse error: {derr}");
                }

                if (server.IsNotFound)
                {
                    if (_local.TryGet(key, out var lj) && _json.TryDeserialize<T>(lj, out var lo, out _))
                        return DataResult<T>.Ok(lo, fromServer: false, fromLocal: true, fromMemory: false);

                    return DataResult<T>.NotFound();
                }

                // network/server fail → fallback local
            }

            // Local fallback
            if (_local.TryGet(key, out var json))
            {
                if (_json.TryDeserialize<T>(json, out var localObj, out var lerr))
                    return DataResult<T>.Ok(localObj, fromServer: false, fromLocal: true, fromMemory: false);
                
                if (!string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(lerr))
                    return DataResult<T>.Error($"Local JSON parse error: {lerr}");
            }

            return DataResult<T>.NotFound();
        }

        public async UniTask<DataResult<bool>> SetData<T>(string key, T value, CancellationToken ct = default)
        {
            ct = DefaultCt(ct);

            var json = _json.Serialize(value);
            
            _local.Set(key, json);
            
            if (IsLoggedIn && _savedata != null)
            {
                var up = await _savedata.UpsertDescriptionJsonByKeyAsync(key, json, ct);
                if (up.Success)
                    return DataResult<bool>.Ok(true, fromServer: true, fromLocal: true, fromMemory: false);

                // soft-fail:
                return DataResult<bool>.Error($"Server save failed: {up.Error} (saved locally)");
            }

            return DataResult<bool>.Ok(true, fromServer: false, fromLocal: true, fromMemory: false);
        }

        public UniTask<DataResult<PlayerProfileJson>> GetPlayerProfileJson(CancellationToken ct = default, bool forceRefresh = false)
            => GetData<PlayerProfileJson>(_ProfileKey, ct, forceRefresh);

        public UniTask<DataResult<bool>> SetPlayerProfileJson(PlayerProfileJson pj, CancellationToken ct = default)
            => SetData(_ProfileKey, pj, ct);


        [Button("DEBUG: GET ProfileJson")]
        private async void Btn_DebugGetProfile()
        {
            if (!Application.isPlaying) return;
            var r = await GetPlayerProfileJson();
            Debug.Log($"[UserCenter][Profile] status={r.Status} fromServer={r.FromServer} fromLocal={r.FromLocal} err={r.ErrorMessage} val={JsonUtility.ToJson(r.Value)}");
        }

        [Button("DEBUG: SET ProfileJson (random)")]
        private async void Btn_DebugSetProfile()
        {
            if (!Application.isPlaying) return;
            var pj = new PlayerProfileJson {username = "User_" + UnityEngine.Random.Range(1000, 9999), avatarId = "default"};
            var r = await SetPlayerProfileJson(pj);
            Debug.Log($"[UserCenter][Profile][SET] status={r.Status} err={r.ErrorMessage}");
        }

        private static string ExtractJwtFromLoginResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            try
            {
                var jo = JToken.Parse(raw);
                return (string) (jo["token"] ?? jo["access_token"] ?? jo["accessToken"]);
            }
            catch
            {
                return null;
            }
        }
    }
}
