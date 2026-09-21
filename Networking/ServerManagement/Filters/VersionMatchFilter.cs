// Author: Michal Petr
// Created: 21.09.2026

using System;
using EDIVE.Core;
using EDIVE.Core.Versions;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.Filters
{
    [Serializable]
    public class VersionMatchFilter : IServerListFilter
    {
        [SerializeField]
        private AppVersionSignificance _Significance = AppVersionSignificance.Patch;

        [SerializeField]
        private bool _AllowUnknownVersion;

        public bool Matches(ServerRecord record)
        {
            if (record.Version == AppVersion.ZERO)
                return _AllowUnknownVersion;
            return record.Version.CompareTo(AppCore.CurrentVersion, _Significance) == 0;
        }
    }
}
