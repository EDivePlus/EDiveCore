// Author: František Holubec
// Created: 02.09.2026

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Rendering.UVDecals
{
    public class UVDecalPreset : ScriptableObject
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<UVDecal> _Decals = new();

        public IReadOnlyList<UVDecal> Decals => _Decals;

#if UNITY_EDITOR
        public static event Action<UVDecalPreset> Changed;

        private void OnValidate() => Changed?.Invoke(this);
#endif
    }
}
