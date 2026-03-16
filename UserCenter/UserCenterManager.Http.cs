// Author: Michal Petr
// Created: 16.03.2026

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.UserCenter.Auth;
using UnityEngine;

namespace EDIVE.UserCenter
{
    public partial class UserCenterManager
    {
        private async UniTask<NetworkResponse<string>> GetAsync(
            string url,
            bool includeAuthHeader = true,
            bool includeBranchHeader = false,
            int timeoutSeconds = 5,
            CancellationToken cancellationToken = default
        )
        {
            return await SendAsync(UnityEngine.Networking.UnityWebRequest.kHttpVerbGET, url, null, includeAuthHeader, includeBranchHeader, timeoutSeconds, cancellationToken);
        }
        
        private async UniTask<NetworkResponse<string>> PostAsync(
            string url,
            string jsonBodyOrNull,
            bool includeAuthHeader = true,
            bool includeBranchHeader = false,
            int timeoutSeconds = 5,
            CancellationToken cancellationToken = default
        )
        {
            return await SendAsync(UnityEngine.Networking.UnityWebRequest.kHttpVerbPOST, url, jsonBodyOrNull, includeAuthHeader, includeBranchHeader, timeoutSeconds, cancellationToken);
        }
        
        private async UniTask<NetworkResponse<string>> PutAsync(
            string url,
            string jsonBodyOrNull,
            bool includeAuthHeader = true,
            bool includeBranchHeader = false,
            int timeoutSeconds = 5,
            CancellationToken cancellationToken = default
        )
        {
            return await SendAsync(UnityEngine.Networking.UnityWebRequest.kHttpVerbPUT, url, jsonBodyOrNull, includeAuthHeader, includeBranchHeader, timeoutSeconds, cancellationToken);
        }
        
        private async UniTask<NetworkResponse<string>> SendAsync(
            string method, 
            string url,
            string jsonBodyOrNull,
            bool includeAuthHeader,
            bool includeBranchHeader,
            int timeoutSeconds,
            CancellationToken cancellationToken
        )
        {
            var token = includeAuthHeader ? AuthStorage.GetAccessToken() : null;
            Dictionary<string, string> headers = null;

            if (includeBranchHeader)
            {
                if (string.IsNullOrWhiteSpace(_BranchId))
                {
                    Debug.LogError("[UserCenterHttp] includeBranchHeader=true but BranchId is NULL/EMPTY. Savedata endpoints will fail (branch is required).");
                }
                else
                {
                    headers = new Dictionary<string, string>
                    {
                        {"branch-id", _BranchId}, {"branchId", _BranchId}, {"branch", _BranchId}, {"Branch-Id", _BranchId}, {"X-Branch-Id", _BranchId}
                    };
                }
            }

            try
            {
                var result = await RestUtils.SendRawRequestAsync(url, method, jsonBodyOrNull, token, headers, Mathf.Max(3, timeoutSeconds), cancellationToken);
                return NetworkResponse<string>.Ok(result.statusCode, result.response, result.response);
            }
            catch (RestRequestException ex)
            {
                var errorMsg = ex.StatusCode == 0 ? "connection error / TLS / offline" : ex.Message;
                return NetworkResponse<string>.Fail(ex.StatusCode, errorMsg, ex.Response);
            }
        }
    }
}
