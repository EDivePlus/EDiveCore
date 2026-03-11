// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [Serializable]
    public class MaterialVisualPresetRecord : AVisualPresetRecord<MaterialVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Material _Material;
        
        public Material Material => _Material;

        public override string EditorLabel => "Material";

        public override bool Equals(AVisualPresetRecord other)
        {
            if (other is not MaterialVisualPresetRecord materialRecord)
                return false;

            return base.Equals(other) && Equals(Material, materialRecord.Material);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Material != null ? Material.GetHashCode() : 0);
        }
    }
}
