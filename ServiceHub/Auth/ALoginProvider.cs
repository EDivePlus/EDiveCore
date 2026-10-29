// Author: František Holubec
// Created: 29.10.2026

using System;
using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.ServiceHub.Auth
{
    // Sign-in provider setup, Id is the provider key on Service Hub
    [Serializable]
    [EnhancedTypeSelector(true, 1)]
    public abstract class ALoginProvider
    {
        public abstract string Id { get; }
    }
}
