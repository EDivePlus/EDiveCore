using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.StateHandling
{
    [Serializable]
    public class ObjectStatePresetField
    {
        [PropertyOrder(10)]
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

#if UNITY_EDITOR
    public sealed class ObjectStatePresetFieldDrawer : OdinValueDrawer<ObjectStatePresetField>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            Property.Children["_ObjectPresets"].Draw(label);
        }
    }
#endif
}
