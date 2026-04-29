// Author: František Holubec
// Created: 29.04.2026

using Cysharp.Threading.Tasks;

namespace EDIVE.Networking.ServerManagement
{
    public abstract class AServerEndpoint
    {
        public string Name;
        
        public abstract string EndpointText { get; }
        public abstract UniTask<bool> PrepareForConnect();

        public override string ToString() => $"{Name}: {EndpointText}";
    }
}
