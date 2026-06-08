using System.Collections;
using System.Collections.Generic;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.StateValuePresets;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.StateHandling
{
    [System.Serializable]
    public class ObjectStatePresetRecord
    {
        [SerializeField]
        [InlineButton("Apply")]
        [EnhancedObjectDrawer(ShowSelectRoot = false)]
        private Object _Target;

        [SerializeReference]
        [HideReferenceObjectPicker]
        [CompactList]
        [EnhancedValidate("ValidateValuePresets", ContinuousValidationCheck = true)]
        [ValueDropdown("GetValuePresetDropdown", IsUniqueList = true, DrawDropdownForListElements = false)]
        internal List<AStateValuePreset> _ValuePresets = new();

        public Object Target
        {
            get => _Target;
            set => _Target = value;
        }

        public List<AStateValuePreset> ValuePresets => _ValuePresets;

        public ObjectStatePresetRecord() { }

        public ObjectStatePresetRecord(Object target)
        {
            _Target = target;
        }

        public ObjectStatePresetRecord(Object target, List<AStateValuePreset> valuePresets)
        {
            _Target = target;
            _ValuePresets = valuePresets;
        }

        public void Apply()
        {
            if (_ValuePresets == null || _Target == null)
                return;
            
            foreach (var valuePreset in _ValuePresets)
            {
                valuePreset?.ApplyTo(_Target);

            }
        }

        public void Capture()
        {
            if (_ValuePresets == null || _Target == null)
                return;

            foreach (var valuePreset in _ValuePresets)
            {
                valuePreset?.CaptureFrom(_Target);
            }
        }

#if UNITY_EDITOR
        public void SetDirty()
        {
            if (_Target == null)
                return;

            EditorUtility.SetDirty(_Target);
        }
        
        [EnhancedTableColumn("Controls", 55)]
        [OnInspectorGUI]
        private void DrawControls(InspectorProperty property)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
            if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.UploadSolid, "Apply"))
            {
                if (Target != null)
                {
                    Undo.RecordObject(Target, "Apply state presets");
                    Apply();
                    SetDirty();
                }
            }
            
            rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
            if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.DownloadSolid, "Capture"))
            {
                if (Target != null)
                {
                    Capture();
                    property.MarkSerializationRootDirty();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        [UsedImplicitly]
        private IEnumerable GetValuePresetDropdown()
        {
            if (Target == null)
                return new List<ValueDropdownItem<AStateValuePreset>>();
            return StateControlEditorUtils.GetValuePresetDropdown(Target.GetType());
        }

        [UsedImplicitly]
        private void ValidateValuePresets(List<AStateValuePreset> value, SelfValidationResult result)
        {
            if (Target == null)
                return;

            StateControlEditorUtils.ValidateStateValuePresets(Target.GetType(), value, result);
        }
#endif
    }
}
