// Author: František Holubec
// Created: 21.03.2025

using System;
using EDIVE.BuildTool.Presets;
using UnityEditor;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class ProductNamePathSegment : ABuildPathSegment
    {
        public override string GetValue(ABuildPreset preset) => PlayerSettings.productName;
    }
}
