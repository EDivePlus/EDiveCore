using System.Collections.Generic;
using EDIVE.OdinExtensions;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
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

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnObjectPresetsTitleBarGUI(List<ObjectStatePresetRecord> value, InspectorProperty property)
        {
            if (OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.UploadSolid, "Apply"))
            {
                foreach (var record in value)
                {
                    if (record.Target == null)
                        continue;
                    Undo.RecordObject(record.Target, "Apply state presets");
                    record.Apply();
                    record.SetDirty();
                }
            }

            if (OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.DownloadSolid, "Capture"))
            {
                foreach (var record in value)
                {
                    if (record.Target == null)
                        continue;
                    record.Capture();
                }
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
