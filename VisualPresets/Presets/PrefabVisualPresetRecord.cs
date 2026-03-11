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

        public override bool Equals(AVisualPresetRecord other)
        {
            if (other is not PrefabVisualPresetRecord prefabRecord)
                return false;
            return base.Equals(other) && _Prefab == prefabRecord._Prefab;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Prefab != null ? Prefab.GetHashCode() : 0);
        }
    }
}
