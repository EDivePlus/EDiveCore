using System;
using System.Collections.Generic;
using System.Linq;
using Adrenak.BRW;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;

namespace Adrenak.UniVoice.Networks
{
    /// <summary>
    /// PurrNet implementation of <see cref="IAudioServer{T}"/>.
    /// Receives BRW-encoded audio packets from clients, filters them through each peer's
    /// <see cref="VoiceSettings"/>, and re-broadcasts to the appropriate recipients.
    /// </summary>
    public class PurrNetServer : IAudioServer<int>
    {
        private const string TAG = "[PurrNetServer]";

        public event Action OnServerStart;
        public event Action OnServerStop;
        public event Action OnClientVoiceSettingsUpdated;

        public List<int> ClientIDs { get; private set; }
        public Dictionary<int, VoiceSettings> ClientVoiceSettings { get; private set; }

        private readonly NetworkManager _networkManager;
        private bool _serverStarted;

        public PurrNetServer()
        {
            ClientIDs = new List<int>();
            ClientVoiceSettings = new Dictionary<int, VoiceSettings>();

            _networkManager = NetworkManager.main;
            if (_networkManager == null)
            {
                Debug.unityLogger.LogError(TAG, "NetworkManager.main is null. PurrNetServer cannot subscribe.");
                return;
            }

            _networkManager.onServerConnectionState += OnServerConnectionStateChanged;
            _networkManager.onPlayerJoined += OnPlayerJoined;
            _networkManager.onPlayerLeft += OnPlayerLeft;
            _networkManager.Subscribe<PurrNetBroadcast>(OnReceivedMessage, asServer: true);
        }

        public void Dispose()
        {
            if (_networkManager)
            {
                _networkManager.onServerConnectionState -= OnServerConnectionStateChanged;
                _networkManager.onPlayerJoined -= OnPlayerJoined;
                _networkManager.onPlayerLeft -= OnPlayerLeft;
                _networkManager.Unsubscribe<PurrNetBroadcast>(OnReceivedMessage, asServer: true);
            }
            OnServerShutdown();
        }

        private void OnServerConnectionStateChanged(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                if (_serverStarted) return;
                _serverStarted = true;
                OnServerStart?.Invoke();
            }
            else if (state == ConnectionState.Disconnected)
            {
                if (!_serverStarted) return;
                _serverStarted = false;
                OnServerShutdown();
            }
        }

        private void OnServerShutdown()
        {
            ClientIDs.Clear();
            ClientVoiceSettings.Clear();
            OnServerStop?.Invoke();
        }

        private void OnPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
        {
            if (!asServer) return;
            OnServerConnected(PlayerIdToInt(player));
        }

        private void OnPlayerLeft(PlayerID player, bool asServer)
        {
            if (!asServer) return;
            OnServerDisconnected(PlayerIdToInt(player));
        }

        private void OnReceivedMessage(PlayerID sender, PurrNetBroadcast message, bool asServer)
        {
            if (!asServer) return;

            var clientId = PlayerIdToInt(sender);
            var reader = new BytesReader(message.data);
            var tag = reader.ReadString();

            if (tag.Equals(PurrNetBroadcastTags.AUDIO_FRAME))
            {
                // Start with every other client as a candidate recipient
                var peersToForwardAudioTo = ClientIDs.Where(x => x != clientId);

                // Apply the sender's outgoing filters (deafening)
                if (ClientVoiceSettings.TryGetValue(clientId, out var senderSettings))
                {
                    // Sender has deafened everyone — drop the frame entirely
                    if (senderSettings.deafenAll)
                        return;

                    // Drop peers the sender has deafened by ID
                    peersToForwardAudioTo = peersToForwardAudioTo
                        .Where(x => !senderSettings.deafenedPeers.Contains(x));

                    // Drop peers the sender has deafened by tag
                    peersToForwardAudioTo = peersToForwardAudioTo.Where(peer =>
                    {
                        if (ClientVoiceSettings.TryGetValue(peer, out var peerVoiceSettings))
                        {
                            var hasDeafenedPeer = senderSettings.deafenedTags
                                .Intersect(peerVoiceSettings.myTags).Any();
                            return !hasDeafenedPeer;
                        }
                        return true;
                    });
                }

                // Then apply each recipient's incoming filters (muting)
                foreach (var recipient in peersToForwardAudioTo)
                {
                    if (ClientVoiceSettings.TryGetValue(recipient, out var recipientSettings))
                    {
                        if (recipientSettings.muteAll) continue;
                        if (recipientSettings.mutedPeers.Contains(clientId)) continue;
                        if (senderSettings != null &&
                            recipientSettings.mutedTags.Intersect(senderSettings.myTags).Any())
                            continue;
                    }
                    SendToClient(recipient, message.data, Channel.Unreliable);
                }
            }
            else if (tag.Equals(PurrNetBroadcastTags.VOICE_SETTINGS))
            {
                var muteAll = reader.ReadInt() == 1;
                var mutedPeers = reader.ReadIntArray().ToList();
                var deafenAll = reader.ReadInt() == 1;
                var deafenedPeers = reader.ReadIntArray().ToList();
                var myTags = reader.ReadStringArray().ToList();
                var mutedTags = reader.ReadStringArray().ToList();
                var deafenedTags = reader.ReadStringArray().ToList();

                ClientVoiceSettings[clientId] = new VoiceSettings
                {
                    muteAll = muteAll,
                    mutedPeers = mutedPeers,
                    deafenAll = deafenAll,
                    deafenedPeers = deafenedPeers,
                    myTags = myTags,
                    mutedTags = mutedTags,
                    deafenedTags = deafenedTags
                };
                OnClientVoiceSettingsUpdated?.Invoke();
            }
        }

        private void OnServerConnected(int connId)
        {
            if (ClientIDs.Contains(connId)) return;
            ClientIDs.Add(connId);
            Debug.unityLogger.Log(LogType.Log, TAG,
                $"Client {connId} connected. IDs now: {string.Join(", ", ClientIDs)}");
        }

        private void OnServerDisconnected(int connId)
        {
            ClientIDs.Remove(connId);
            ClientVoiceSettings.Remove(connId);
            Debug.unityLogger.Log(LogType.Log, TAG,
                $"Client {connId} disconnected. IDs now: {string.Join(", ", ClientIDs)}");
        }

        private void SendToClient(int clientConnId, byte[] bytes, Channel channel)
        {
            if (!TryGetPlayer(clientConnId, out var player))
                return;

            var message = new PurrNetBroadcast { data = bytes };
            _networkManager.Send(player, message, channel);
        }

        private bool TryGetPlayer(int desiredClientID, out PlayerID resultPlayer)
        {
            foreach (var p in _networkManager.players)
            {
                if (PlayerIdToInt(p) == desiredClientID)
                {
                    resultPlayer = p;
                    return true;
                }
            }
            resultPlayer = default;
            return false;
        }

        private static int PlayerIdToInt(PlayerID id) => (int)id.id.value;
    }
}
