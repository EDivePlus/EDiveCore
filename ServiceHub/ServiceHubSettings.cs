// Author: Michal Petr
// Created: 12.05.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Time.TimeSpanUtils;
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
        [EnhancedBoxGroup("SaveData", Color = "@ColorTools.Green")]
        private UTimeSpan _DirtyDataSyncInterval = TimeSpan.FromSeconds(30);

        public string ServiceBaseUrl => (_ServiceBaseUrl ?? "").TrimEnd('/');
        public string AppSecret => _AppSecret ?? "";
        public int ApiTimeoutSeconds => Mathf.Max(3, _ApiTimeoutSeconds);
        public int AuthTimeoutSeconds => Mathf.Max(3, _AuthTimeoutSeconds);
        public TimeSpan DirtyDataSyncInterval => _DirtyDataSyncInterval;
    }
}
