// Author: František Holubec
// Created: 29.10.2026

using System;
using EDIVE.ServiceHub.Auth;

namespace EDIVE.Networking.ServiceHub
{
    // Signed in to Service Hub
    [Serializable]
    public class SignInRequirement : ARequirement
    {
        public override bool IsMet => AuthStorage.Client.HasSession;

        protected override void OnBind() => AuthStorage.Client.SessionChanged += NotifyChanged;
        protected override void OnUnbind() => AuthStorage.Client.SessionChanged -= NotifyChanged;
    }
}
