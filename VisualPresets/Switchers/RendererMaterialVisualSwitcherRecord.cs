// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.VisualPresets.Switchers
{
    [Serializable]
    public class RendererMaterialVisualSwitcherRecord : AVisualSwitcherRecord<MaterialVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Renderer _Renderer;
        
        [VerticalGroup("Value")]
        [MinValue(0)]
        [SerializeField]
        private int _MaterialIndex;
        
        [VerticalGroup("Value")]
        [SerializeField]
        private bool _UseSharedMaterial;
        
        public override string EditorLabel => "Renderer Material";
        public override Type EditorIconTargetType => typeof(MeshRenderer);

        public Renderer Renderer => _Renderer;
        public int MaterialIndex => _MaterialIndex;
        public bool UseSharedMaterial => _UseSharedMaterial;
    }
    
    [Preserve]
    public class RendererMaterialTextVisualSwitcherStrategy : AVisualSwitcherStrategy<MaterialVisualID, MaterialVisualPresetRecord, RendererMaterialVisualSwitcherRecord>
    {
        protected override IDisposable Apply(MaterialVisualPresetRecord presetRecord, RendererMaterialVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Renderer == null)
                return DisposableUtils.Empty;
            
            var materials = switcherRecord.UseSharedMaterial ? switcherRecord.Renderer.sharedMaterials : switcherRecord.Renderer.materials;
            if (materials.Length <= 0)
                return DisposableUtils.Empty;

            if (switcherRecord.MaterialIndex < 0 || switcherRecord.MaterialIndex >= materials.Length)
                return DisposableUtils.Empty;

            materials[switcherRecord.MaterialIndex] = presetRecord.Material; 
            if (switcherRecord.UseSharedMaterial)
                switcherRecord.Renderer.sharedMaterials = materials;
            else
                switcherRecord.Renderer.materials = materials;
            
            return DisposableUtils.Empty;
        }
    }
}
