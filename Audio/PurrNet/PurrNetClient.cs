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
    /// PurrNet implementation of <see cref="IAudioClient{T}"/>.
    /// Mirrors the FishNet implementation: identifies peers via int IDs derived from <see cref="PlayerID"/>,
    /// uses BRW to encode payloads, and routes them through a single typed broadcast (<see cref="PurrNetBroadcast"/>).
    /// </summary>
    public class PurrNetClient : IAudioClient<PlayerID>
    {
        private const string TAG = "[PurrNetClient]";

        public PlayerID ID => _id ?? default;
        public List<PlayerID> PeerIDs { get; private set; }
        public VoiceSettings YourVoiceSettings { get; private set; }

        public event Action<PlayerID, List<PlayerID>> OnJoined;
        public event Action OnLeft;
        public event Action<PlayerID> OnPeerJoined;
        public event Action<PlayerID> OnPeerLeft;
        public event Action<PlayerID, AudioFrame> OnReceivedPeerAudioFrame;

        private PlayerID? _id = null;
        private readonly NetworkManager _networkManager;

        public PurrNetClient()
        {
            PeerIDs = new List<PlayerID>();
            YourVoiceSettings = new VoiceSettings();

            _networkManager = NetworkManager.main;
            if (_networkManager == null)
            {
                Debug.unityLogger.LogError(TAG, "NetworkManager.main is null. PurrNetClient cannot subscribe.");
                return;
            }

            _networkManager.onClientConnectionState += OnClientConnectionStateChanged;
            _networkManager.onLocalPlayerReceivedID += OnLocalPlayerReceivedID;
            _networkManager.onPlayerJoined += OnPlayerJoined;
            _networkManager.onPlayerLeft += OnPlayerLeft;
            _networkManager.Subscribe<PurrNetBroadcast>(OnReceivedMessage, asServer: false);
        }

        public void Dispose()
        {
            if (_networkManager != null)
            {
                _networkManager.onClientConnectionState -= OnClientConnectionStateChanged;
                _networkManager.onLocalPlayerReceivedID -= OnLocalPlayerReceivedID;
                _networkManager.onPlayerJoined -= OnPlayerJoined;
                _networkManager.onPlayerLeft -= OnPlayerLeft;
                _networkManager.Unsubscribe<PurrNetBroadcast>(OnReceivedMessage, asServer: false);
            }
            PeerIDs.Clear();
        }

        private void OnPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
        {
            // We only want the client-side view of who else is in the voice chat.
            if (asServer) return;

            // Until we know our own ID we cannot tell which join refers to ourselves.
            if (_id == null) return;

            var peerId = player;
            if (peerId == ID) return;
            if (PeerIDs.Contains(peerId)) return;

            PeerIDs.Add(peerId);
            Debug.unityLogger.Log(LogType.Log, TAG,
                $"Peer {peerId} joined. Peer list is now {string.Join(", ", PeerIDs)}");
            OnPeerJoined?.Invoke(peerId);
        }

        private void OnPlayerLeft(PlayerID player, bool asServer)
        {
            if (asServer) return;
            
            if (!PeerIDs.Remove(player)) return;

            var log = $"Peer {player} left. ";
            log += PeerIDs.Count == 0
                ? "There are no peers anymore."
                : $"Peer list is now {string.Join(", ", PeerIDs)}";
            Debug.unityLogger.Log(LogType.Log, TAG, log);
            OnPeerLeft?.Invoke(player);
        }

        // PurrNet equivalent of FishNet's OnClientAuthenticated: local client has been assigned its PlayerID.
        private void OnLocalPlayerReceivedID(PlayerID player)
        {
            PeerIDs = _networkManager.players
                .Where(p => p != player)
                .ToList();

            var log = $"Initialized with ID {ID}. ";
            log += PeerIDs.Count > 0
                ? $"Peer list: {string.Join(", ", PeerIDs)}"
                : "There are currently no peers.";
            Debug.unityLogger.Log(LogType.Log, TAG, log);

            OnJoined?.Invoke(ID, PeerIDs);
            foreach (var peerId in PeerIDs)
                OnPeerJoined?.Invoke(peerId);
        }

        private void OnClientConnectionStateChanged(ConnectionState state)
        {
            if (state != ConnectionState.Disconnected) return;

            YourVoiceSettings = new VoiceSettings();
            var oldPeerIds = PeerIDs.ToList();
            PeerIDs.Clear();
            _id = null;
            foreach (var peerId in oldPeerIds)
                OnPeerLeft?.Invoke(peerId);
            OnLeft?.Invoke();
        }

        private void OnReceivedMessage(PlayerID sender, PurrNetBroadcast msg, bool asServer)
        {
            var reader = new BytesReader(msg.data);
            var tag = reader.ReadString();
            switch (tag)
            {
                // Audio relayed by the server, originating from another peer
                case PurrNetBroadcastTags.AUDIO_FRAME:
                    var senderId = ReadPlayerID(reader);
                    if (senderId == ID || !PeerIDs.Contains(senderId))
                        return;
                    var frame = new AudioFrame
                    {
                        timestamp = reader.ReadLong(),
                        frequency = reader.ReadInt(),
                        channelCount = reader.ReadInt(),
                        samples = reader.ReadByteArray()
                    };
                    OnReceivedPeerAudioFrame?.Invoke(senderId, frame);
                    break;
            }
        }

        /// <summary>
        /// Sends an audio frame captured on this client to the server.
        /// </summary>
        public void SendAudioFrame(AudioFrame frame)
        {
            if (_id == null) return;
            if (_networkManager == null || !_networkManager.isClient) return;

            var writer = new BytesWriter();
            writer.WriteString(PurrNetBroadcastTags.AUDIO_FRAME);
            WritePlayerID(writer, ID);
            writer.WriteLong(frame.timestamp);
            writer.WriteInt(frame.frequency);
            writer.WriteInt(frame.channelCount);
            writer.WriteByteArray(frame.samples);

            var message = new PurrNetBroadcast { data = writer.Bytes };
            _networkManager.SendToServer(message, Channel.Unreliable);
        }

        /// <summary>
        /// Pushes the current <see cref="YourVoiceSettings"/> to the server.
        /// </summary>
        public void SubmitVoiceSettings()
        {
            if (_id == null) return;
            if (_networkManager == null || !_networkManager.isClient) return;

            Debug.unityLogger.Log(TAG, "Submitting : " + YourVoiceSettings);

            var writer = new BytesWriter();
            writer.WriteString(PurrNetBroadcastTags.VOICE_SETTINGS);
            writer.WriteInt(YourVoiceSettings.muteAll ? 1 : 0);
            writer.WriteIntArray(YourVoiceSettings.mutedPeers.ToArray());
            writer.WriteInt(YourVoiceSettings.deafenAll ? 1 : 0);
            writer.WriteIntArray(YourVoiceSettings.deafenedPeers.ToArray());
            writer.WriteStringArray(YourVoiceSettings.myTags.ToArray());
            writer.WriteStringArray(YourVoiceSettings.mutedTags.ToArray());
            writer.WriteStringArray(YourVoiceSettings.deafenedTags.ToArray());

            var message = new PurrNetBroadcast { data = writer.Bytes };
            _networkManager.SendToServer(message);
        }

        public void UpdateVoiceSettings(Action<VoiceSettings> modification)
        {
            modification?.Invoke(YourVoiceSettings);
            SubmitVoiceSettings();
        }
        
        public void WritePlayerID(BytesWriter writer, PlayerID value) {
            var bytes = BitConverter.GetBytes(value.id);
            EndianUtility.EndianCorrection(bytes);
            writer.WriteBytes(bytes);
            writer.WriteByte(value.isBot ? (byte) 1 : (byte) 0);
        }
        
        public PlayerID ReadPlayerID(BytesReader reader)
        {
            var idBytes = reader.ReadBytes(8);
            var isBot = reader.ReadBytes(1)[0] == 1;
            EndianUtility.EndianCorrection(idBytes);
            return new PlayerID(BitConverter.ToUInt64(idBytes, 0), isBot);
        }
    }
}
