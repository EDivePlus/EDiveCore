// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Core.Versions;
using EDIVE.Networking.ServerManagement.PurrNet;
using EDIVE.Networking.Utils;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.Lobby;
using PurrNet;
using PurrNet.Transports;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.Networking.ServerManagement.ServiceHub
{
    public class ServiceHubServerListAdapter : AServerListAdapter
    {
        [SerializeField]
        [MinValue(1f)]
        [Tooltip("How often the lobby is queried while searching is active.")]
        private float _QueryInterval = 4f;
        
        [SerializeField]
        [MinValue(1f)]
        [Tooltip("How many lobbies to fetch per query.")]
        private int _QueryCount = 5;
        
        [SerializeField]
        [MinValue(1f)]
        [Tooltip("Interval between heartbeat updates. Backend default TTL is ~40s.")]
        private float _HeartbeatInterval = 15f;

        [SerializeField]
        [MinValue(1)]
        [Tooltip("Timeout (seconds) for the lobby registration request. The TLS handshake on a freshly started dedicated server can be slow, so this is intentionally higher than the default API timeout.")]
        private int _RegisterTimeoutSeconds = 15;

        [SerializeField]
        [MinValue(0f)]
        [Tooltip("Base delay (seconds) for the exponential backoff between failed registration attempts.")]
        private float _RegisterRetryBaseDelay = 2f;

        [SerializeField]
        [MinValue(1f)]
        [Tooltip("Maximum delay (seconds) between failed registration attempts.")]
        private float _RegisterRetryMaxDelay = 30f;

        [SerializeField]
        [Tooltip("DIAGNOSTIC ONLY. Bypass TLS certificate validation for the registration request to test whether registration timeouts are caused by certificate validation/revocation. Leave OFF in production.")]
        private bool _BypassCertificateValidation;

        private ServiceHubManager _serviceHub;
        private LobbyService Lobby => _serviceHub.Lobby;
        
        private CancellationTokenSource _searchCancellation;
        private CancellationTokenSource _heartbeatCancellation;
        private CancellationTokenSource _probeCancellation;
        
        private float _lastQueryTime;
        private AServerEndpoint[] _localEndpoints;
        
        private string _serverSecret;
        private string _joinCode;
            
        private bool _disposing;
        private DateTime _lastSuccessfulContactUtc;
        
        private string _purrRelayRoom = string.Empty;
        private bool _relayInitialized;

        public override UniTask Initialize()
        {
            base.Initialize();
            _serviceHub = AppCore.Services.Get<ServiceHubManager>();
            return UniTask.CompletedTask;
        }

        public override async UniTask PrepareServerStart()
        {
            await base.PrepareServerStart();

            if (!_serviceHub.HasValidSetup)
                return;
            
            if (NetworkManager.main.transport is LocalTransport)
            {
                Debug.LogWarning("[ServiceHubServerListAdapter] Server hosted as local, skipping lobby registration.");
                return;
            }

            InitializeRelay();

            // First attempt happens here so the join code is available as early as possible.
            // If it fails, the heartbeat loop keeps retrying with backoff instead of giving up.
            await RegisterLobby(destroyCancellationToken);

            _heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            HeartbeatTask(_heartbeatCancellation.Token).Forget();
        }

        private void InitializeRelay()
        {
            if (_relayInitialized)
                return;

            if (NetworkManager.main.TryGetCurrentTransport<PurrTransport>(out var purrTransport))
            {
                _purrRelayRoom = Guid.NewGuid().ToString().Replace("-", "");
                purrTransport.roomName = _purrRelayRoom;
            }

            _relayInitialized = true;
        }
        
        private void OnDestroy()
        {
            _disposing = true;
            DisposeAsync().AsTask();
        }

        public override void StopServer()
        {
            base.StopServer();
            if (!_disposing)
                DisposeAsync().Forget();
        }
        
        public override void StartSearch()
        {
            if (_searchCancellation != null)
                return;
            
            _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            SearchTask(_searchCancellation.Token).Forget();
        }
        
        public override void StopSearch()
        {
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _searchCancellation = null;
        }

        public override void RegisterHostServer(ServerRecord hostServer)
        {
            base.RegisterHostServer(hostServer);
            
            if (hostServer == null || _localEndpoints == null)
                return;

            hostServer.Endpoints.AddRange(_localEndpoints);
            hostServer.JoinCode = _joinCode;
        }

        public override async UniTask<(bool, ServerRecord)> TryHandleJoinRequest(IJoinRequest request)
        {
            if (request is not CodeJoinRequest codeRequest)
                return (false, null);
            
            var response = await Lobby.GetServerAsync(codeRequest.Code, CancellationToken.None);
            if (!response.IsSuccess || response.Result == null)                
                return (false, null);
            
            var record = BuildRecords(new[] { response.Result }).FirstOrDefault();
            return (record != null, record);
        }

        private async UniTaskVoid SearchTask(CancellationToken cancellationToken)
        {
            if (!_serviceHub.HasValidSetup)
                return;
            
            // Ensure we don't spam the lobby service with queries if search is stopped and started again quickly.
            if (UnityEngine.Time.realtimeSinceStartup - _lastQueryTime < 2f)
                await UniTask.Delay(TimeSpan.FromSeconds(2), true, cancellationToken: cancellationToken);
                
            while (!cancellationToken.IsCancellationRequested)
            {
                _lastQueryTime = UnityEngine.Time.realtimeSinceStartup;
                var response = await Lobby.QueryServersAsync(
                    new QueryServersRequest { Count = _QueryCount, Skip = 0, Version = AppCore.CurrentVersion.ToBaseString(), VersionSegments = 3},
                    cancellationToken);
                var records = response.IsSuccess && response.Result != null
                    ? BuildRecords(response.Result)
                    : Array.Empty<ServerRecord>();
                SetServers(records);
                await UniTask.Delay(TimeSpan.FromSeconds(_QueryInterval), true, cancellationToken: cancellationToken);
            }
        }

        private static IEnumerable<ServerRecord> BuildRecords(IEnumerable<LobbyServerResponse> lobbies)
        {
            foreach (var lobby in lobbies)
            {
                var endpoints = new List<AServerEndpoint>();
                if (!string.IsNullOrWhiteSpace(lobby.PublicAddress) && lobby.PublicPort > 0)
                {
                    endpoints.Add(new AddressServerEndpoint
                    {
                        Name = "Remote Direct",
                        Address = lobby.PublicAddress,
                        Port = (ushort) lobby.PublicPort
                    });
                }
                
                var data = ServiceHubServerData.TryParse(lobby.Data);
                if (data == null || string.IsNullOrEmpty(data.InstanceID))
                    continue;
                
                if (!string.IsNullOrEmpty(data.PurrRelayRoom))
                {
                    endpoints.Add(new PurrRelayServerEndpoint
                    {
                        Name = "Purr Relay",
                        RoomName = data.PurrRelayRoom
                    });
                }
                
                yield return new ServerRecord
                {
                    InstanceID = data.InstanceID,
                    ServerName = lobby.Name,
                    CurrentPlayers = lobby.CurrentPlayers,
                    MaxPlayers = lobby.MaxPlayers,
                    Endpoints = endpoints,
                    JoinCode = lobby.JoinCode,
                    Version = AppVersion.FromBaseString(lobby.Version)
                };
            }
        }

        private async UniTask<bool> RegisterLobby(CancellationToken cancellationToken = default)
        {
            if (!_serviceHub.HasValidSetup)
                return false;

            var serverManager = AppCore.Services.Get<NetworkServerManager>();

            var data = new ServiceHubServerData
            {
                InstanceID = serverManager.InstanceID,
                PurrRelayRoom = _purrRelayRoom
            };

            var certificateHandler = _BypassCertificateValidation ? new BypassCertificateHandler() : null;

            var response = await Lobby.RegisterServerAsync(new RegisterServerRequest
            {
                Name = _serverConfig.ServerName,
                Version = AppCore.CurrentVersion.ToBaseString(),
                PublicAddress = _serverConfig.PublicAddress,
                PublicPort = serverManager.ResolvedPort,
                MaxPlayers = _serverConfig.MaxPlayers,
                Data = data.Serialize(),
                IsPrivate = _serverConfig.IsPrivate,
                IsDebug = Debug.isDebugBuild
            }, _RegisterTimeoutSeconds, certificateHandler, cancellationToken);

            if (!response.IsSuccess || response.Result == null)
            {
                Debug.LogError($"[ServiceHubServerListAdapter] Server registration failed: {response.ErrorMessage}");
                return false;
            }
            _serverSecret = response.Result.Secret;
            _joinCode = response.Result.Code;

            if (_currentServerRecord != null)
                _currentServerRecord.JoinCode = _joinCode;

            _lastSuccessfulContactUtc = DateTime.UtcNow;

            var endpoints = new List<AServerEndpoint>();
            if (!string.IsNullOrEmpty(_serverConfig.PublicAddress) && serverManager.ResolvedPort > 0)
            {
                endpoints.Add(new AddressServerEndpoint
                {
                    Name = "Remote Direct",
                    Address = _serverConfig.PublicAddress,
                    Port = serverManager.ResolvedPort,
                });
            }

            if (!string.IsNullOrEmpty(_purrRelayRoom))
            {
                endpoints.Add(new PurrRelayServerEndpoint
                {
                    Name = "Purr Relay",
                    RoomName = _purrRelayRoom
                });
            }

            _localEndpoints = endpoints.ToArray();
            Debug.Log($"[ServiceHubServerListAdapter] Server registered (join code: {_joinCode}).");
            return true;
        }
        
        private async UniTaskVoid HeartbeatTask(CancellationToken cancellationToken)
        {
            var registerAttempt = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                // (Re)registration path: runs whenever we don't hold a valid secret — i.e. the initial
                // registration failed, or the lobby entry was lost. Retries with exponential backoff so a
                // transient failure (e.g. a slow TLS handshake on startup) no longer permanently drops the
                // server from the lobby.
                if (string.IsNullOrEmpty(_serverSecret))
                {
                    if (await RegisterLobby(cancellationToken))
                    {
                        registerAttempt = 0;
                    }
                    else
                    {
                        registerAttempt++;
                        var delay = Mathf.Min(_RegisterRetryMaxDelay, _RegisterRetryBaseDelay * Mathf.Pow(2f, registerAttempt - 1));
                        Debug.LogWarning($"[ServiceHubServerListAdapter] Registration attempt {registerAttempt} failed — retrying in {delay:0.#}s.");
                        await UniTask.Delay(TimeSpan.FromSeconds(delay), true, cancellationToken: cancellationToken);
                        continue;
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, _HeartbeatInterval)), true, cancellationToken: cancellationToken);

                if (string.IsNullOrEmpty(_serverSecret))
                    continue;

                try
                {
                    var currentPlayers = NetworkManager.main.playerCount;

                    var request = new UpdateServerRequest{
                        Secret = _serverSecret,
                        Name = _serverConfig.ServerName,
                        CurrentPlayers = currentPlayers
                    };

                    var response = await Lobby.UpdateServerAsync(request, cancellationToken);
                    if (response.IsSuccess)
                    {
                        _lastSuccessfulContactUtc = DateTime.UtcNow;
                    }
                    else if (response.ApiStatus is (int) ServiceHubStatus.InvalidSecret or (int) ServiceHubStatus.ServerNotFound)
                    {
                        Debug.Log($"[ServiceHubServerListAdapter] Lobby entry no longer valid (api status {response.ApiStatus}: {response.ErrorMessage}) — re-registering server.");
                        _serverSecret = null;
                    }
                    else
                    {
                        Debug.LogError($"[ServiceHubServerListAdapter] Heartbeat/update failed (status {response.StatusCode}): {response.ErrorMessage}");
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    if ((DateTime.UtcNow - _lastSuccessfulContactUtc).TotalSeconds >= Mathf.Max(1f, _HeartbeatInterval))
                    {
                        Debug.Log($"[ServiceHubServerListAdapter] Lobby entry lost (exception: {e.Message}) — re-registering server.");
                        _serverSecret = null;
                    }
                    else
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        private async UniTask DisposeAsync()
        {
            _heartbeatCancellation?.Cancel();
            _heartbeatCancellation?.Dispose();
            _heartbeatCancellation = null;
            _localEndpoints = null;
            
            if (string.IsNullOrEmpty(_serverSecret) || Lobby == null)
                return;

            var secret = _serverSecret;
            _serverSecret = null;

            try
            {
                var response = await Lobby.DisposeServerAsync(secret, CancellationToken.None);
                if (!response.IsSuccess)
                    Debug.LogWarning($"[ServiceHubServerListAdapter] Dispose failed: {response.ErrorMessage}");
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogWarning($"[ServiceHubServerListAdapter] Dispose exception: {e.Message}");
            }
        }
    }

    /// <summary>
    /// DIAGNOSTIC ONLY certificate handler that accepts any TLS certificate. Used to test whether
    /// registration timeouts are caused by certificate validation/revocation in Unity's TLS stack.
    /// Never use this for anything that requires real transport security.
    /// </summary>
    internal class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
