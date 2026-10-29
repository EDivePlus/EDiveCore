// Author: František Holubec
// Created: 29.10.2026

using System;
using UnityEngine;

namespace EDIVE.ServiceHub.Auth
{
    [Serializable]
    public class MetaLoginProvider : ALoginProvider
    {
        public const string ID = "meta";

        [SerializeField]
        [Tooltip("Meta Horizon app ID")]
        private string _AppId;

        public override string Id => ID;
        public string AppId => _AppId;
    }
}
