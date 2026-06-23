// Author: František Holubec

using System;
using PurrNet.Packing;

namespace EDIVE.Networking.Scenes
{
    public readonly struct SceneKey : IPackedAuto, IEquatable<SceneKey>
    {
        public readonly string Id;
        public readonly bool IsAddressable;
        
        public SceneKey(string id, bool isAddressable)
        {
            Id = id;
            IsAddressable = isAddressable;
        }

        public bool IsValid => !string.IsNullOrEmpty(Id);

        public bool Equals(SceneKey other) => IsAddressable == other.IsAddressable && Id == other.Id;
        public override bool Equals(object obj) => obj is SceneKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(IsAddressable, Id);
        public override string ToString() => IsAddressable ? $"Addressable:{Id}" : $"Direct:{Id}";
    }
}
