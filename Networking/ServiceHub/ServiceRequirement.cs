// Author: František Holubec
// Created: 29.10.2026

using System;
using EDIVE.ServiceHub;
using UnityEngine;

namespace EDIVE.Networking.ServiceHub
{
    // Service turned on for the app in Service Hub
    [Serializable]
    public class ServiceRequirement : ARequirement
    {
        [SerializeField]
        private AppService _Service = AppService.Content;

        public override bool IsMet => AppServices.IsEnabled(_Service);

        protected override void OnBind() => AppServices.Changed += NotifyChanged;
        protected override void OnUnbind() => AppServices.Changed -= NotifyChanged;
    }
}
