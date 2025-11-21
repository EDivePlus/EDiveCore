// Author: František Holubec
// Created: 21.11.2025

using Cysharp.Threading.Tasks;

namespace EDIVE.Networking.ServerManagement
{
    public abstract class AServerRecord
    {
        public long ServerID;
        public string ServerName;
        public int MaxPlayers;
        public int CurrentPlayers;
        public string ConnectType;

        public abstract UniTask<bool> PrepareForConnect();
    }
}
