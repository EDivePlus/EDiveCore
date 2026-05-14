// Author: Michal Petr
// Created: 13.05.2026

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.Probe
{
    public class ProbeService : MonoBehaviour, IServiceHubModule
    {
        protected ServiceHubSettings Settings { get; private set; }

        private string ProbeBaseUrl => $"{Settings.ServiceBaseUrl}/probe";
        private string CheckUrl => $"{ProbeBaseUrl}/check";

        private int RequestTimeoutSeconds => Settings.ApiTimeoutSeconds;

        public void Initialize(ServiceHubSettings settings)
        {
            Settings = settings;
        }

        [Button]
        [PropertyOrder(99)]
        [EnhancedBoxGroup("Probe", Color = "@ColorTools.Aqua", SpaceBefore = 8)]
        public async UniTask<ProbeResult> CheckPortAsync(int port, int timeoutMs = 3000, CancellationToken cancellationToken = default)
        {
            if (port is <= 0 or > ushort.MaxValue)
                return ProbeResult.Unreachable;

            UdpClient udp;
            try
            {
                udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            }
            catch (SocketException e)
            {
                Debug.LogWarning($"[ServiceHub] Probe could not bind UDP port {port}: {e.Message}");
                return ProbeResult.Unreachable;
            }
            
            using var echoCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, cancellationToken);
            try
            {
                var token = Guid.NewGuid().ToString("N");
                EchoLoop(udp, token, echoCts.Token).Forget();

                var request = new ProbeRequest(port, token, timeoutMs);
                var httpTimeout = Mathf.Max(RequestTimeoutSeconds, (timeoutMs / 1000) + 3);
                var response = await RestUtils.PostAsync<ApiResponse<ProbeResponse>, ProbeRequest>(
                    CheckUrl,
                    request,
                    authToken: null,
                    headers: null,
                    timeout: httpTimeout,
                    cancellationToken: cancellationToken
                );

                var unwrapped = ApiResponseHelper.UnwrapApi(response, $"ProbePort({port})");
                if (!unwrapped.IsSuccess || unwrapped.Result == null)
                    return ProbeResult.Unreachable;

                return new ProbeResult(unwrapped.Result.Reachable, unwrapped.Result.PublicAddress);
            }
            catch (OperationCanceledException)
            {
                return ProbeResult.Unreachable;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ServiceHub] Probe failed: {e.Message}");
                return ProbeResult.Unreachable;
            }
            finally
            {
                echoCts.Cancel();
                udp.Close();
            }
        }

        private static async UniTaskVoid EchoLoop(UdpClient udp, string expectedToken, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var received = await udp.ReceiveAsync().AsUniTask().AttachExternalCancellation(cancellationToken);
                    var text = Encoding.UTF8.GetString(received.Buffer);
                    if (text == expectedToken)
                        await udp.SendAsync(received.Buffer, received.Buffer.Length, received.RemoteEndPoint);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                Debug.LogWarning($"[ServiceHub] Probe echo loop error: {e.Message}");
            }
        }
    }
}
