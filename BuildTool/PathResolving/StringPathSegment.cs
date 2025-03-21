// Author: František Holubec
// Created: 21.03.2025

using System;
using EDIVE.BuildTool.Presets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class StringPathSegment : ABuildPathSegment
    {
        [HideLabel]
        [SerializeField]
        [LabelText("String")]
        public string _Value;

        public override string GetValue(ABuildPreset preset) => _Value;
        protected override bool HideLabel => true;
    }
}
