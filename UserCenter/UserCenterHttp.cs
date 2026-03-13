// Author: Radim Holub
// Created: 19.02.2026

using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.UserCenter
{
    internal sealed class UserCenterHttp
    {
        private readonly Func<string> _tokenOrNull;
        private readonly Func<bool> _branchEnabled;
        private readonly Func<string> _branchIdOrNull;

        public UserCenterHttp(Func<string> tokenOrNull, Func<bool> branchEnabled, Func<string> branchIdOrNull)
        {
            _tokenOrNull = tokenOrNull;
            _branchEnabled = branchEnabled;
            _branchIdOrNull = branchIdOrNull;
        }

        public async UniTask<NetworkResponse<string>> SendRawAsync(
            string method,
            string url,
            string jsonBodyOrNull,
            bool includeAuthHeader,
            bool includeBranchHeader,
            int timeoutSeconds,
            CancellationToken ct
        )
        {
            using var req = new UnityWebRequest(url, method);

            if (!string.IsNullOrEmpty(jsonBodyOrNull))
            {
                var bytes = Encoding.UTF8.GetBytes(jsonBodyOrNull);
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.SetRequestHeader("Content-Type", "application/json");
            }

            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Accept", "application/json");
            req.timeout = Mathf.Max(3, timeoutSeconds);

            if (includeAuthHeader)
            {
                var token = _tokenOrNull?.Invoke();
                if (!string.IsNullOrEmpty(token))
                    req.SetRequestHeader("Authorization", "Bearer " + token);
            }

            if (includeBranchHeader)
            {
                var bid = _branchIdOrNull?.Invoke();

                if (string.IsNullOrWhiteSpace(bid))
                { 
                    Debug.LogError("[UserCenterHttp] includeBranchHeader=true but BranchId is NULL/EMPTY. Savedata endpoints will fail (branch is required).");
                }
                else
                {
                    req.SetRequestHeader("branch-id", bid);
                    req.SetRequestHeader("branchId", bid);
                    req.SetRequestHeader("branch", bid);
                    req.SetRequestHeader("Branch-Id", bid);
                    req.SetRequestHeader("X-Branch-Id", bid);
                }
            }

            await req.SendWebRequest().ToUniTask(cancellationToken: ct);

            var raw = req.downloadHandler?.text ?? "";
            if (req.result == UnityWebRequest.Result.Success)
                return NetworkResponse<string>.Ok(req.responseCode, raw, raw);

            var err = req.error;
            if (req.responseCode == 0 && req.result == UnityWebRequest.Result.ConnectionError)
                err = "connection error / TLS / offline";

            return NetworkResponse<string>.Fail(req.responseCode, err, raw);
        }
    }
}

