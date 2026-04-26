// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [Serializable]
    public class PrefabVisualPresetRecord : AVisualPresetRecord<PrefabVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private GameObject _Prefab;
        
        public GameObject Prefab => _Prefab;

        public override string EditorLabel => "Prefab";

        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is PrefabVisualPresetRecord prefabRecord && _Prefab == prefabRecord._Prefab;
        }

        public override int GetHashCodeInternal()
        {
            return Prefab != null ? Prefab.GetHashCode() : 0;
        }
    }
}
