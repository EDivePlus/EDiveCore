// Author: Michal Petr
// Created: 15.06.2026

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
    public class RendererMaterialColorVisualSwitcherRecord : AVisualSwitcherRecord<ColorVisualID>
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
        
        public override string EditorLabel => "Renderer Color";
        public override Type EditorIconTargetType => typeof(MeshRenderer);
        
        public bool TryGetMaterial(out Material material)
        {
            var materials = _UseSharedMaterial ? _Renderer.sharedMaterials : _Renderer.materials;

#if UNITY_EDITOR
            // To not leak materials into the scene
            if (!Application.isPlaying) 
                materials = _Renderer.sharedMaterials;
#endif
            
            material = null;

            if (materials.Length == 0 || _MaterialIndex >= materials.Length)
                return false;

            material = materials[_MaterialIndex];
            return true;
        }
    }
    
    [Preserve]
    public class RendererMaterialColorVisualSwitcherStrategy : AVisualSwitcherStrategy<ColorVisualID, ColorVisualPresetRecord, RendererMaterialColorVisualSwitcherRecord>
    {
        protected override IDisposable Apply(ColorVisualPresetRecord presetRecord, RendererMaterialColorVisualSwitcherRecord switcherRecord)
        {
            if (!switcherRecord.TryGetMaterial(out var material))
                return DisposableUtils.Empty;
            
            material.color = presetRecord.Color;
            return DisposableUtils.Empty;
        }
    }
}
