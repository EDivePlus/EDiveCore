// Author: František Holubec
// Created: 21.10.2025

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Conditions.Impl
{
    [Serializable]
    public class PlatformCondition : ABoolCondition
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<RuntimePlatform> _Platforms = new();
        
        protected override bool GetValue() => _Platforms.Contains(Application.platform);
    }
}
