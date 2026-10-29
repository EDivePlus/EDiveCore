// Author: František Holubec
// Created: 29.10.2026

using System;
using UnityEngine;

namespace EDIVE.ServiceHub.Auth
{
    [Serializable]
    public class GoogleLoginProvider : ALoginProvider
    {
        public const string ID = "google";

        [SerializeField]
        [Tooltip("OAuth web client ID, the audience of the ID token")]
        private string _ClientId;

        public override string Id => ID;
        public string ClientId => _ClientId;
    }
}
