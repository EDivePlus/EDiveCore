// Author: František Holubec
// Created: 28.04.2026

namespace EDIVE.Networking.ServerManagement
{
    public abstract class ADirectServerRecord : AServerRecord
    {
        public string DirectAddress;
        public ushort DirectPort;
    }
}
