// Author: František Holubec
// Created: 08.08.2025

using System;

namespace EDIVE.Networking.ServerManagement.LocalNetwork
{
    [Serializable]
    public class NetworkDiscoveryResponse
    {
        public string InstanceID;
        public string ServerName;
        public ushort Port;
        public int MaxPlayers;
        public int CurrentPlayers;
    }
}
