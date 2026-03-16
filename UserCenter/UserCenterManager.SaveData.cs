// Author: Michal Petr
// Created: 16.03.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.UserCenter.Auth;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.UserCenter
{
    public partial class UserCenterManager
    {
        private readonly SemaphoreSlim _indexLock = new(1, 1);
        private readonly Dictionary<string, SaveDataRecord> _index = new(StringComparer.OrdinalIgnoreCase);
        private bool _indexLoaded;

        private void InvalidateIndex() => _indexLoaded = false;
        
        private string SaveDataBaseUrl()
        {
            var baseUrl = (_ServiceBaseUrl ?? "").TrimEnd('/');
            return $"{baseUrl}/saveDatas";
        }
        
        private static List<SaveDataRecord> TryParseList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            try
            {
                var direct = JsonConvert.DeserializeObject<List<SaveDataRecord>>(raw);
                if (direct != null)
                    return direct;

                // todo: remove content wrap
                var wrapper = JsonConvert.DeserializeObject<ContentWrapper<SaveDataRecord>>(raw);
                return wrapper?.Content;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse save data list response: {e}");
            }
            return null;
        }

        private async UniTask<NetworkResponse<string>> ListRawAsync(CancellationToken ct)
        {
            var listUrl = SaveDataBaseUrl() + "?page=0&size=250";
            return await GetAsync(listUrl, true, true, _ApiTimeoutSeconds, ct);
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

                var userUuid = AuthStorage.GetUserId();
                if (!string.IsNullOrEmpty(userUuid))
                {
                    rows = rows.FindAll(r =>
                        string.Equals(r.UserUuid, userUuid, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r.UserBasicPojo?.Uuid, userUuid, StringComparison.OrdinalIgnoreCase));
                }

                foreach (var r in rows.Where(r => !string.IsNullOrWhiteSpace(r.Key)))
                {
                    _index[r.Key] = r;
                }

                _indexLoaded = true;
            }
            finally
            {
                _indexLock.Release();
            }
        }

        private async UniTask<NetworkResponse<string>> GetDescriptionJsonByKeyAsync(string key, CancellationToken ct, bool forceRefresh)
        {
            await EnsureIndexAsync(ct, forceRefresh);

            if (!_indexLoaded)
                return NetworkResponse<string>.Fail(0, "index not loaded (network error)", raw: null);

            if (!_index.TryGetValue(key ?? "", out var rec) || rec == null || string.IsNullOrEmpty(rec.Description))
                return NetworkResponse<string>.Fail(404, "not found", raw: null);

            return NetworkResponse<string>.Ok(200, rec.Description, rec.Description);
        }

        private async UniTask<NetworkResponse<string>> UpsertDescriptionJsonByKeyAsync(string key, string descriptionJson, CancellationToken ct)
        {
            await EnsureIndexAsync(ct, force: false);
            
            _index.TryGetValue(key ?? "", out var existing);

            if (existing != null && existing.ID > 0)
            {
                var putUrl = SaveDataBaseUrl() + "/" + existing.ID;
                var body = JsonConvert.SerializeObject(new {key, description = descriptionJson });
                var put = await PutAsync(putUrl, body, true, true, _ApiTimeoutSeconds, ct);
                if (put.Success) InvalidateIndex();
                return put;
            }
            else
            {
                var userUuid = AuthStorage.GetUserId();

                object obj = string.IsNullOrEmpty(userUuid)
                    ? new { key, description = descriptionJson }
                    : new { key, description = descriptionJson, userUuid };

                var body = JsonConvert.SerializeObject(obj);

                var post = await PostAsync(SaveDataBaseUrl(), body, true, true, _ApiTimeoutSeconds, ct);

                if (post.Success) InvalidateIndex();
                return post;
            }
        }
    }
}
