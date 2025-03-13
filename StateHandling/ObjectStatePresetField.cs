using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.StateHandling
{
    [System.Serializable]
    public class ObjectStatePresetField
    {
        [PropertyOrder(10)]
        [ListDrawerSettings(OnTitleBarGUI = "OnObjectPresetsTitleBarGUI")]
        [SerializeField]
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
                if (objectPreset == null)
                {
                    Debug.LogError("Object preset is null!");
                    continue;
                }
                objectPreset.Apply();
            }
        }

#if UNITY_EDITOR
        public void OnObjectPresetsTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton("APPLY"))
            {
                Apply();
            }
        }
#endif
    }
}
