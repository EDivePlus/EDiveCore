// Author: František Holubec
// Created: 21.03.2025

using System;
using EDIVE.BuildTool.Presets;
using EDIVE.Core.Versions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class VersionPathSegment : ABuildPathSegment
    {
        [Indent]
        [SerializeField]
        private bool _OverrideFormat;

        [Indent]
        [HideLabel]
        [ShowIf(nameof(_OverrideFormat))]
        [SerializeField]
        private AppVersionFormat _Format;

        public override string GetValue(ABuildPreset preset)
        {
            var version = BuildGlobalSettings.Instance.VersionDefinition;
            return _OverrideFormat ? version.CurrentVersion.GetFormatedString(_Format) : version.VersionString;
        }
    }
}
