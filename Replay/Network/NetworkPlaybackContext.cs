// Author: František Holubec
// Created: 16.02.2026

using FishNet.Managing;

namespace EDIVE.Replay.Network
{
    public class NetworkPlaybackContext : PlaybackContext
    {
        public NetworkManager NetworkManager { get; }

        public NetworkPlaybackContext(NetworkManager networkManager)
        {
            NetworkManager = networkManager;
        }
    }
}
