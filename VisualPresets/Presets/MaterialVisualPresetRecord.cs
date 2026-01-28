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
        
        public Material Material
        {
            get => _Material;
            set => _Material = value;
        }
        
        public override string EditorLabel => "Material";

    }
}
