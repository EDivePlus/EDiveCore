// Author: Michal Petr
// Created: 12.05.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.ServiceHub.Auth;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.TimeHandling.TimeSpanUtils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub
{
    [CreateAssetMenu(menuName = "EDIVE/ServiceHub/ServiceHubSettings", fileName = "ServiceHubSettings")]
    public class ServiceHubSettings : ScriptableObject
    {
        [SerializeField]
        [EnhancedBoxGroup("Endpoint", Color = "@ColorTools.Cyan")]
        private string _ServiceBaseUrl = "https://ediveplus.phil.muni.cz/api";

        [SerializeField]
        [EnhancedBoxGroup("Endpoint")]
        private string _AppSecret = "";

        [SerializeField]
        [EnhancedBoxGroup("Timeouts", Color = "@ColorTools.Orange")]
        [MinValue(1)]
        private int _ApiTimeoutSeconds = 5;

        [SerializeField]
        [EnhancedBoxGroup("Timeouts")]
        [MinValue(1)]
        private int _AuthTimeoutSeconds = 5;

        [SerializeField]
        [EnhancedBoxGroup("Timeouts")]
        [MinValue(10)]
        [Tooltip("Whole download of shared content files")]
        private int _ContentTimeoutSeconds = 120;

        [SerializeField]
        [EnhancedBoxGroup("SaveData", Color = "@ColorTools.Green")]
        private UTimeSpan _BatchSaveDataSyncInterval = TimeSpan.FromSeconds(30);

        [SerializeField]
        [EnhancedBoxGroup("Presence", Color = "@ColorTools.Purple")]
        [Tooltip("Reports this app to ServiceHub and receives its commands. Needed for streaming.")]
        private bool _PresenceEnabled;

        [SerializeReference]
        [EnhancedBoxGroup("Login", Color = "@ColorTools.Yellow")]
        [Tooltip("Sign-in providers shown on the login screen")]
        private List<ALoginProvider> _LoginProviders = new();

        public string ServiceBaseUrl => (_ServiceBaseUrl ?? "").TrimEnd('/');
        public string AppSecret => _AppSecret ?? "";
        public int ApiTimeoutSeconds => Mathf.Max(3, _ApiTimeoutSeconds);
        public int AuthTimeoutSeconds => Mathf.Max(3, _AuthTimeoutSeconds);
        public int ContentTimeoutSeconds => Mathf.Max(10, _ContentTimeoutSeconds);
        public TimeSpan BatchSaveDataSyncInterval => _BatchSaveDataSyncInterval;
        public bool PresenceEnabled => _PresenceEnabled;

        public bool TryGetLoginProvider(string id, out ALoginProvider provider)
        {
            provider = _LoginProviders.FirstOrDefault(p => p != null && p.Id == id);
            return provider != null;
        }
    }
}
