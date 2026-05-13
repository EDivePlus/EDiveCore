// Author: Michal Petr
// Created: 13.05.2026

namespace EDIVE.ServiceHub.Probe
{
    public readonly struct ProbeResult
    {
        public bool Reachable { get; }
        public string PublicAddress { get; }

        public ProbeResult(bool reachable, string publicAddress)
        {
            Reachable = reachable;
            PublicAddress = publicAddress;
        }

        public static ProbeResult Unreachable => new(false, string.Empty);
    }
}
