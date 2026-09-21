// Author: Michal Petr
// Created: 21.09.2026

using System.Collections.Generic;
using EDIVE.AssetTranslation;
using EDIVE.Rendering.UVDecals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.UVDecals
{
    public class UVDecalDefinition : AUniqueDefinition
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<UVDecal> _Decals = new();

        public IReadOnlyList<UVDecal> Decals => _Decals;

        [ShowInInspector]
        [PreviewField(64)]
        [PropertyOrder(-1)]
        public Texture2D Preview => _Decals.Count > 0 ? _Decals[0]._Texture : null;
    }
}
