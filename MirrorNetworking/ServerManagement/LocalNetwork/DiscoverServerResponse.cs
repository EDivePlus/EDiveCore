// Author: František Holubec
// Created: 14.07.2025

using System;
using Mirror;

namespace EDIVE.MirrorNetworking.ServerManagement.LocalNetwork
{
    public struct DiscoverServerResponse : NetworkMessage
    {
        public Uri Address;
        
        public long ServerID;
        public string ServerName;
        public int MaxPlayers;
        public int CurrentPlayers;
    }
}
