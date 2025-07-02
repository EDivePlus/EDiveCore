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

namespace EDIVE.StateHandling.ToggleStates
{
    [System.Serializable]
    public class ToggleStateRecord
    {
        [Required]
        [SerializeField]
        [DontValidate]
        [EnhancedObjectDrawer]
        private Object _Target;

        [SerializeReference]
        [HorizontalGroup("Presets")]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = "OnPresetsTitleBarGUI")]
        [OnValueChanged("OnPresetsChanged", true)]
        [EnhancedValidate("ValidateValuePresets", ContinuousValidationCheck = true)]
        [ValueDropdown("GetValuePresetDropdown", IsUniqueList = true, DrawDropdownForListElements = false)]
        internal List<AStateValuePreset> _EnabledPresets = new();

        [SerializeReference]
        [HorizontalGroup("Presets")]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = "OnPresetsTitleBarGUI")]
        [OnValueChanged("OnPresetsChanged", true)]
        [EnhancedValidate("ValidateValuePresets", ContinuousValidationCheck = true)]
        [ValueDropdown("GetValuePresetDropdown", IsUniqueList = true, DrawDropdownForListElements = false)]
        internal List<AStateValuePreset> _DisabledPresets = new();

        public Object Target { get => _Target; set => _Target = value; }

        public virtual bool State { get => _state; set => SetState(value); }

        private bool _state;

        public ToggleStateRecord() { }
        public ToggleStateRecord(Object target) { _Target = target; }

        public virtual void SetState(bool state)
        {
            _state = state;
            if (_Target == null)
                return;

            var valuePresets = _state ? _EnabledPresets : _DisabledPresets;
            Apply(valuePresets);

#if UNITY_EDITOR
            EditorUtility.SetDirty(_Target);
#endif
        }

        private void Apply(List<AStateValuePreset> presets)
        {
            if (presets == null || _Target == null)
                return;

            foreach (var preset in presets)
            {
                preset?.ApplyTo(_Target);
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(_Target);
#endif
        }

        private void Capture(List<AStateValuePreset> presets)
        {
            if (presets == null || _Target == null)
                return;

            foreach (var preset in presets)
            {
                preset?.CaptureFrom(_Target);
            }
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnPresetsTitleBarGUI(List<AStateValuePreset> value, InspectorProperty property)
        {
            if (OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.UploadSolid, "Apply"))
            {
                if (Target != null)
                {
                    Undo.RecordObject(Target, "Apply state presets");
                    Apply(value);
                }
            }

            if (OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.DownloadSolid, "Capture"))
            {
                if (Target != null)
                {
                    Capture(value);
                    property.MarkSerializationRootDirty();
                }
            }
        }

        [UsedImplicitly]
        private void OnPresetsChanged(List<AStateValuePreset> value, InspectorProperty property)
        {
            var otherValue = value == _EnabledPresets ? _DisabledPresets : _EnabledPresets;
            if (value == null || otherValue == null)
                return;

            var changed = false;
            foreach (var preset in value)
            {
                if (preset == null)
                    continue;

                if (!otherValue.Contains(preset))
                {
                    otherValue.Add(preset.GetCopy());
                    changed = true;
                }
            }

            if (changed)
                property.MarkSerializationRootDirty();
        }

        [UsedImplicitly]
        private IEnumerable GetValuePresetDropdown()
        {
            if (Target == null) return null;
            return StateControlEditorUtils.GetValuePresetDropdown(Target.GetType());
        }
        
        [UsedImplicitly]
        private void ValidateValuePresets(List<AStateValuePreset> value, SelfValidationResult result)
        {
            if (Target == null) return;
            StateControlEditorUtils.ValidateStateValuePresets(Target.GetType(), value, result); 
        }
#endif
    }
}
