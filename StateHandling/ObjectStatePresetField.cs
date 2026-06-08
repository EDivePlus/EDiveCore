using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
#endif

namespace EDIVE.StateHandling
{
    [HideLabel]
    [InlineProperty]
    [Serializable]
    public class ObjectStatePresetField
    {
        [PropertyOrder(10)]
        [LabelText("@$property.Parent.NiceName")]
        [SerializeField]
        [EnhancedTableList(HideToolbar = true)]
        private List<ObjectStatePresetRecord> _ObjectPresets = new();

        public IReadOnlyList<ObjectStatePresetRecord> ObjectPresets => _ObjectPresets;

        public ObjectStatePresetField() { }
        public ObjectStatePresetField(List<ObjectStatePresetRecord> objectPresets)
        {
            _ObjectPresets = objectPresets;
        }

        public void Apply()
        {
            foreach (var objectPreset in _ObjectPresets)
            {
                objectPreset?.Apply();
            }
        }

        public void Capture()
        {
            foreach (var objectPreset in _ObjectPresets)
            {
                objectPreset?.Capture();
            }
        }
    }
}
