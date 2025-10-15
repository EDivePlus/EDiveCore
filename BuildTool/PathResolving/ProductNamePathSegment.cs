// Author: František Holubec
// Created: 21.03.2025

using System;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class ProductNamePathSegment : ABuildPathSegment
    {
        public override string GetValue(BuildPreset preset) => PlayerSettings.productName.Replace(" ", "");
    }
}
