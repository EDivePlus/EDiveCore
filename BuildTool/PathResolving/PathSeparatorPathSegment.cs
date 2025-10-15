// Author: František Holubec
// Created: 21.03.2025

using System;
using System.IO;
using Sirenix.OdinInspector;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class PathSeparatorPathSegment : ABuildPathSegment
    {
        public override string GetValue(BuildPreset preset) => Path.DirectorySeparatorChar.ToString();
        protected override bool HideLabel => true;

        [EnableGUI]
        [ShowInInspector]
        public string Separator => "\\";
    }
}
