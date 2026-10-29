// Author: František Holubec
// Created: 29.10.2026

using System;
using UnityEngine;

namespace EDIVE.ServiceHub.Auth
{
    [Serializable]
    public class AppleLoginProvider : ALoginProvider
    {
        public const string ID = "apple";

        [SerializeField]
        [Tooltip("Sign in with Apple services ID")]
        private string _ServiceId;

        public override string Id => ID;
        public string ServiceId => _ServiceId;
    }
}
