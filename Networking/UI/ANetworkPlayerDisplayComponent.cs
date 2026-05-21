// Author: Michal Petr
// Created: 21.05.2026

using System;
using EDIVE.Networking.Players;

namespace EDIVE.Networking.UI
{
    [Serializable]
    public abstract class ANetworkPlayerDisplayComponent
    {
        public NetworkPlayerController PlayerController { get; private set; }

        public virtual void InitializeForPlayer(NetworkPlayerController playerController)
        {
            PlayerController = playerController;
        }

        public virtual void UpdateDisplay() { }

        public virtual void RegisterListeners() { }
        public virtual void UnregisterListeners() { }
    }
}
