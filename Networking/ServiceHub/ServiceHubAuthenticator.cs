// Author: Michal Petr
// Created: 10.09.2026

using System.Threading.Tasks;
using EDIVE.NativeUtils;
using EDIVE.ServiceHub.Auth;
using PurrNet;
using PurrNet.Authentication;
using PurrNet.Transports;
using UnityEngine;

namespace EDIVE.Networking.ServiceHub
{
    [RegisterNetworkType(typeof(AuthenticationRequest<string>))]
    public class ServiceHubAuthenticator : AuthenticationBehaviour<string>
    {
        protected override Task<AuthenticationRequest<string>> GetClientPayload()
        {
            var userId = ClientAuthService.GetUserId();
            var installId = ClientAuthService.GetOrCreateAnonymousToken();
#if UNITY_EDITOR
            // Fix for editor and ParallelSync
            installId += Application.dataPath;
#endif
            var cookie = HashUtils.Sha256Hex($"{userId}:{installId}");

            Debug.Log($"ServiceHubAuthenticator: GetClientPayload userId={userId}, installId={installId}, cookie={cookie}");
            return Task.FromResult(new AuthenticationRequest<string>(userId) { cookie = cookie });
        }

        protected override Task<AuthenticationResponse> ValidateClientPayload(Connection conn, string payload)
        {
            // Accept everything, the cookie from the request ties the PlayerID to the logged-in user.
            return Task.FromResult(new AuthenticationResponse { success = true });
        }

        protected override void UnAuthenticateClient(Connection conn) { }
    }
}
