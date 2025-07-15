using System.Collections;
using System.Collections.Generic;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.StateValuePresets;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.StateHandling
{
    [System.Serializable]
    public class ObjectStatePresetRecord 
    {
        [Required]
        [SerializeField]
        [InlineButton("Apply")]
        [EnhancedObjectDrawer]
        private Object _Target;

        [SerializeReference]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = "OnPresetsTitleBarGUI")]
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
        
        [UsedImplicitly]
        private void OnPresetsTitleBarGUI(List<AStateValuePreset> value, InspectorProperty property)
        {
            if (OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.UploadSolid, "Apply"))
            {
                if (Target != null)
                {
                    Undo.RecordObject(Target, "Apply state presets");
                    Apply();
                    SetDirty();
                }
            }

            if (OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.DownloadSolid, "Capture"))
            {
                if (Target != null)
                {
                    Capture();
                    property.MarkSerializationRootDirty();
                }
            }
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
