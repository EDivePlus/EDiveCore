// Author: František Holubec
// Created: 21.03.2025

using System;
using System.IO;
using EDIVE.BuildTool.Presets;
using Sirenix.OdinInspector;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class PathSeparatorPathSegment : ABuildPathSegment
    {
        public override string GetValue(ABuildPreset preset) => Path.DirectorySeparatorChar.ToString();
        protected override bool HideLabel => true;

        [EnableGUI]
        [ShowInInspector]
        public string Separator => "/";
    }
}
