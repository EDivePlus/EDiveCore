// Author: Radim Holub
// Created: 19.02.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace EDIVE.UserCenter
{
    internal sealed class SavedataAPI
    {
        [Serializable]
        private class SavedataRecord
        {
            public long id;
            public string key;
            public string description;
            public string userUuid;
            public UserBasicPojo userBasicPojo;
        }

        [Serializable]
        private class UserBasicPojo
        {
            public string uuid;
        }

        [Serializable]
        private class ContentWrapper<TItem>
        {
            public List<TItem> content;
        }

        private readonly UserCenterHttp _http;
        private readonly Func<string> _userUuid;
        private readonly string _savedataBaseUrl;
        private readonly int _timeoutSeconds;

        private readonly SemaphoreSlim _indexLock = new(1, 1);
        private readonly Dictionary<string, SavedataRecord> _index = new(StringComparer.OrdinalIgnoreCase);
        private bool _indexLoaded;

        public SavedataAPI(UserCenterHttp http, string savedataBaseUrl, Func<string> userUuid, int timeoutSeconds)
        {
            _http = http;
            _savedataBaseUrl = (savedataBaseUrl ?? "").TrimEnd('/');
            _userUuid = userUuid;
            _timeoutSeconds = timeoutSeconds;
        }

        public void InvalidateIndex() => _indexLoaded = false;

        private static List<SavedataRecord> TryParseList(string raw)
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

        private async UniTask<NetworkResponse<string>> ListRawAsync(CancellationToken ct)
        {
            var listUrl = _savedataBaseUrl + "?page=0&size=250";
            return await _http.SendRawAsync(
                method: UnityEngine.Networking.UnityWebRequest.kHttpVerbGET,
                url: listUrl,
                jsonBodyOrNull: null,
                includeAuthHeader: true,
                includeBranchHeader: true,
                timeoutSeconds: _timeoutSeconds,
                ct: ct
            );
        }

        private async UniTask EnsureIndexAsync(CancellationToken ct, bool force)
        {
            if (_indexLoaded && !force) return;

            await _indexLock.WaitAsync(ct);
            try
            {
                if (_indexLoaded && !force) return;

                _index.Clear();

                var resp = await ListRawAsync(ct);
                if (resp.IsNotFound)
                {
                    _indexLoaded = true; // empty
                    return;
                }
                if (!resp.Success)
                {
                    return;
                }

                var rows = TryParseList(resp.Raw);
                if (rows == null || rows.Count == 0)
                {
                    _indexLoaded = true;
                    return;
                }

                var myUuid = _userUuid?.Invoke();
                if (!string.IsNullOrEmpty(myUuid))
                {
                    rows = rows.FindAll(r =>
                        string.Equals(r.userUuid, myUuid, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r.userBasicPojo?.uuid, myUuid, StringComparison.OrdinalIgnoreCase));
                }

                foreach (var r in rows)
                {
                    if (!string.IsNullOrWhiteSpace(r.key))
                        _index[r.key] = r;
                }

                _indexLoaded = true;
            }
            finally
            {
                _indexLock.Release();
            }
        }

        public async UniTask<NetworkResponse<string>> GetDescriptionJsonByKeyAsync(string key, CancellationToken ct, bool forceRefresh)
        {
            await EnsureIndexAsync(ct, forceRefresh);

            if (!_indexLoaded)
                return NetworkResponse<string>.Fail(0, "index not loaded (network error)", raw: null);

            if (!_index.TryGetValue(key ?? "", out var rec) || rec == null || string.IsNullOrEmpty(rec.description))
                return NetworkResponse<string>.Fail(404, "not found", raw: null);

            return NetworkResponse<string>.Ok(200, rec.description, rec.description);
        }

        public async UniTask<NetworkResponse<string>> UpsertDescriptionJsonByKeyAsync(string key, string descriptionJson, CancellationToken ct)
        {
            await EnsureIndexAsync(ct, force: false);
            
            _index.TryGetValue(key ?? "", out var existing);

            if (existing != null && existing.id > 0)
            {
                var putUrl = _savedataBaseUrl + "/" + existing.id;
                var body = JsonConvert.SerializeObject(new { key = key, description = descriptionJson });

                var put = await _http.SendRawAsync(
                    UnityEngine.Networking.UnityWebRequest.kHttpVerbPUT,
                    putUrl,
                    body,
                    includeAuthHeader: true,
                    includeBranchHeader: true,
                    timeoutSeconds: _timeoutSeconds,
                    ct: ct
                );

                if (put.Success) InvalidateIndex();
                return put;
            }
            else
            {
                var postUrl = _savedataBaseUrl;
                var uuid = _userUuid?.Invoke();

                object obj = string.IsNullOrEmpty(uuid)
                    ? new { key = key, description = descriptionJson }
                    : new { key = key, description = descriptionJson, userUuid = uuid };

                var body = JsonConvert.SerializeObject(obj);

                var post = await _http.SendRawAsync(
                    UnityEngine.Networking.UnityWebRequest.kHttpVerbPOST,
                    postUrl,
                    body,
                    includeAuthHeader: true,
                    includeBranchHeader: true,
                    timeoutSeconds: _timeoutSeconds,
                    ct: ct
                );

                if (post.Success) InvalidateIndex();
                return post;
            }
        }
    }
}

