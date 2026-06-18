// Author: František Holubec
// Created: 18.06.2026

using System;
using PurrNet;

namespace EDIVE.Networking.Utils
{
    public class NetworkOwnerObserver : NetworkIdentity
    {
        public event Action OwnershipChanged;

        protected override void OnSpawned() => OwnershipChanged?.Invoke();
        protected override void OnDespawned() => OwnershipChanged?.Invoke();
        protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer) => OwnershipChanged?.Invoke();
        protected override void OnOwnerDisconnected(PlayerID ownerId) => OwnershipChanged?.Invoke();
        protected override void OnOwnerReconnected(PlayerID ownerId) => OwnershipChanged?.Invoke();
    }
}
