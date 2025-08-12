#if FISHNET
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Adrenak.BRW;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;

namespace Adrenak.UniVoice.Networks
{
    public class FishNetServer : IAudioServer<int>
    {
        private const string TAG = "[FishNetServer]";

        public event Action OnServerStart;
        public event Action OnServerStop;
        public event Action OnClientVoiceSettingsUpdated;

        public List<int> ClientIDs { get; private set; }
        public Dictionary<int, VoiceSettings> ClientVoiceSettings { get; private set; }
        
        private NetworkManager _networkManager;
        private List<int> _startedTransports = new();
        private bool _isStarted = false;

        public FishNetServer()
        {
            ClientIDs = new List<int>();
            ClientVoiceSettings = new Dictionary<int, VoiceSettings>();

            _networkManager = InstanceFinder.NetworkManager;
            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionStateChanged;
            _networkManager.ServerManager.OnRemoteConnectionState += OnServerRemoteConnectionStateChanged;
            _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionStateChanged;
            _networkManager.ServerManager.RegisterBroadcast<FishNetMessage>(OnReceivedMessage, false);
        }

        public void Dispose()
        {
            if (_networkManager)
            {
                _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionStateChanged;
                _networkManager.ServerManager.OnRemoteConnectionState -= OnServerRemoteConnectionStateChanged;
                _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionStateChanged;
                _networkManager.ServerManager.UnregisterBroadcast<FishNetMessage>(OnReceivedMessage);
            }
            OnServerShutdown();
        }

        private void OnServerStarted()
        {
            _isStarted = true;
            OnServerStart?.Invoke();
        }

        private void OnServerShutdown()
        {
            _isStarted = false;
            ClientIDs.Clear();
            ClientVoiceSettings.Clear();
            OnServerStop?.Invoke();
        }
        
        private void OnServerRemoteConnectionStateChanged(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Started)
            {
                OnServerConnected(connection.ClientId);
            }
            else if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                OnServerDisconnected(connection.ClientId);
            }
        }

        private void OnServerConnectionStateChanged(ServerConnectionStateArgs args)
        {
            // Connection can change for each transport, so we need to track them
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                _startedTransports.Add(args.TransportIndex);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                _startedTransports.Remove(args.TransportIndex);
            }

            if (!_isStarted && _startedTransports.Count > 0)
            {
                OnServerStarted();
            }
            else if (_isStarted && _startedTransports.Count == 0)
            {
                OnServerShutdown();
            }
        }

        private void OnClientConnectionStateChanged(ClientConnectionStateArgs args)
        {
            // TODO - do we need to check if host or is this enough?
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                OnServerConnected(0);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                OnServerDisconnected(0);
            }
        }

        private void OnReceivedMessage(NetworkConnection connection, FishNetMessage message, Channel channel)
        {
            var clientId = connection.ClientId;
            var reader = new BytesReader(message.data);
            var tag = reader.ReadString();

            if (tag.Equals(FishNetMessageTags.AUDIO_FRAME))
            {
                // We start with all the peers except the one that's
                // sent the audio 
                var peersToForwardAudioTo = ClientIDs
                    .Where(x => x != clientId);

                // Consider the voice settings of the sender to see who
                // the sender doesn't want to send audio to
                if (ClientVoiceSettings.TryGetValue(clientId, out var senderSettings))
                {
                    // If the client sending the audio has deafened everyone
                    // to their audio, we simply return
                    if (senderSettings.deafenAll)
                        return;

                    // Else, we remove all the peers that the sender has
                    // deafened themselves to
                    peersToForwardAudioTo = peersToForwardAudioTo
                        .Where(x => !senderSettings.deafenedPeers.Contains(x));
                }

                // We iterate through each recipient peer that the sender wants to send
                // audio to, checking if they have muted the sender in which case
                // we skip that recipient
                foreach (var receiver in peersToForwardAudioTo)
                {
                    if (ClientVoiceSettings.TryGetValue(receiver, out var receiverSettings))
                    {
                        if (receiverSettings.muteAll)
                            continue;
                        if (receiverSettings.mutedPeers.Contains(clientId))
                            continue;
                    }
                    
                    SendToClient(receiver, message.data, Channel.Unreliable);
                }
            }
            else if (tag.Equals(FishNetMessageTags.VOICE_SETTINGS))
            {
                //Debug.unityLogger.Log(LogType.Log, TAG, "FishNet server stopped");
                // We create the VoiceSettings object by reading from the reader
                // and update the peer voice settings map
                var muteAll = reader.ReadInt() == 1 ? true : false;
                var mutedPeers = reader.ReadIntArray().ToList();
                var deafenAll = reader.ReadInt() == 1 ? true : false;
                var deafenedPeers = reader.ReadIntArray().ToList();
                var voiceSettings = new VoiceSettings
                {
                    muteAll = muteAll,
                    mutedPeers = mutedPeers,
                    deafenAll = deafenAll,
                    deafenedPeers = deafenedPeers
                };
                ClientVoiceSettings[clientId] = voiceSettings;
                OnClientVoiceSettingsUpdated?.Invoke();
            }
        }

        private void OnServerConnected(int connId)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, $"Client {connId} connected");
            ClientIDs.Add(connId);
        }

        private void OnServerDisconnected(int connId)
        {
            ClientIDs.Remove(connId);
            Debug.unityLogger.Log(LogType.Log, TAG, $"Client {connId} disconnected");
        }

        private void SendToClient(int clientConnId, byte[] bytes, Channel channel)
        {
            if (!TryGetConnectionToClient(clientConnId, out var connection)) 
                return;
            
            // Do we require auth?
            var message = new FishNetMessage {data = bytes};
            _networkManager.ServerManager.Broadcast(connection, message, true, channel);
        }

        private bool TryGetConnectionToClient(int desiredClientID, out NetworkConnection resultConnection)
        {
            resultConnection = null;
            foreach (var (clientID, conn) in _networkManager.ServerManager.Clients)
            {
                if (clientID == desiredClientID)
                {
                    resultConnection = conn;
                    return true;
                } 
            }
            return false;
        }
    }
}
#endif
