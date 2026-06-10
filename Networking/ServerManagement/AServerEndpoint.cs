// Author: František Holubec
// Created: 29.04.2026

using System;
using Cysharp.Threading.Tasks;

namespace EDIVE.Networking.ServerManagement
{
    public abstract class AServerEndpoint : IEquatable<AServerEndpoint>
    {
        public string Name;

        public abstract string EndpointText { get; }
        public abstract UniTask<bool> PrepareForConnect();

        public override string ToString() => $"{Name}: {EndpointText}";
        
        public bool Equals(AServerEndpoint other)
        {
            return other != null
                && GetType() == other.GetType()
                && Name == other.Name
                && EndpointText == other.EndpointText;
        }

        public override bool Equals(object obj) => Equals(obj as AServerEndpoint);

        public override int GetHashCode() => HashCode.Combine(GetType(), Name, EndpointText);
    }
}
