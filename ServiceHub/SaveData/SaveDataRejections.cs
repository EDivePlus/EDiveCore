// Author: František Holubec
// Created: 28.10.2026

using System;
using System.Collections.Generic;
using EDIVE.External.DomainReloadHelper;

namespace EDIVE.ServiceHub.SaveData
{
    public readonly struct SaveDataRejection
    {
        public readonly string Domain;
        public readonly string Key;
        public readonly string Reason;
        public readonly DateTime Time;

        public SaveDataRejection(string domain, string key, string reason)
        {
            Domain = domain;
            Key = key;
            Reason = reason;
            Time = DateTime.UtcNow;
        }
    }

    // Writes the server refused for good. The value stays local and is sent again once it changes
    public static class SaveDataRejections
    {
        [ClearOnReload]
        private static Dictionary<(string Domain, string Key), SaveDataRejection> _rejections;

        [ClearOnReload]
        public static event Action Changed;

        private static Dictionary<(string Domain, string Key), SaveDataRejection> Rejections => _rejections ??= new Dictionary<(string Domain, string Key), SaveDataRejection>();

        public static bool Any => _rejections is { Count: > 0 };
        public static IEnumerable<SaveDataRejection> All => Rejections.Values;

        // Retrying can't help, the value itself is refused
        public static bool IsPermanent(int apiStatus) => (ServiceHubStatus) apiStatus is
            ServiceHubStatus.KeyTooLong or ServiceHubStatus.ValueTooLarge or ServiceHubStatus.SaveDataQuotaExceeded or ServiceHubStatus.BatchTooLarge;

        public static string Describe(int apiStatus, string message) => (ServiceHubStatus) apiStatus switch
        {
            ServiceHubStatus.SaveDataQuotaExceeded => "storage limit of the app is full",
            ServiceHubStatus.ValueTooLarge => "value too large",
            ServiceHubStatus.KeyTooLong => "key too long",
            ServiceHubStatus.BatchTooLarge => "batch too large",
            _ => string.IsNullOrEmpty(message) ? "rejected by the server" : message
        };

        internal static void Add(SaveDataRejection rejection)
        {
            Rejections[(rejection.Domain, rejection.Key)] = rejection;
            Changed?.Invoke();
        }

        internal static void Remove(string domain, string key)
        {
            if (_rejections != null && _rejections.Remove((domain, key)))
                Changed?.Invoke();
        }

        internal static void ClearDomain(string domain)
        {
            if (_rejections == null || RemoveDomain(_rejections, domain) == 0)
                return;
            Changed?.Invoke();
        }

        private static int RemoveDomain(Dictionary<(string Domain, string Key), SaveDataRejection> rejections, string domain)
        {
            var keys = new List<(string, string)>();
            foreach (var key in rejections.Keys)
            {
                if (key.Domain == domain)
                    keys.Add(key);
            }
            keys.ForEach(k => rejections.Remove(k));
            return keys.Count;
        }
    }
}
