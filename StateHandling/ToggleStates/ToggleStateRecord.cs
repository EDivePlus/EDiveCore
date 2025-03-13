using System.Collections;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.StateValuePresets;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
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
        
        [PropertySpace(4)]
        [SerializeReference]
        [HideReferenceObjectPicker] 
        [EnhancedValidate("ValidateValuePresets", ContinuousValidationCheck = true)]
        [ValueDropdown("GetValuePresetDropdown", IsUniqueList = true, DrawDropdownForListElements = false)]
        internal List<AStateValuePreset> _EnabledPresets = new();
        
        [SerializeReference]
        [HideReferenceObjectPicker] 
        [EnhancedValidate("ValidateValuePresets", ContinuousValidationCheck = true)]
        [ValueDropdown("GetValuePresetDropdown", IsUniqueList = true, DrawDropdownForListElements = false)]
        internal List<AStateValuePreset> _DisabledPresets = new();
        
        public Object Target
        {
            get => _Target; 
            set => _Target = value;
        }

        [ShowInInspector]
        public virtual bool State
        {
            get => _state;
            set => SetState(value);
        }
        
        private bool _state;

        public ToggleStateRecord() { }
        public ToggleStateRecord(Object target)
        {
            _Target = target;
        }
        
        public virtual void SetState(bool state)
        {
            _state = state;
            if (_Target == null) 
                return;
            
            var valuePresets = _state ? _EnabledPresets : _DisabledPresets;
            if (valuePresets == null)
                return;
            
            for (var index = 0; index < valuePresets.Count; index++)
            {
                var valuePreset = valuePresets[index];
                if (valuePreset == null)
                {
                    Debug.LogError($"[{GetType().Name}] Value preset in '{_Target.name}' at index {index} is null!", _Target);
                    continue;
                }

                valuePreset.ApplyTo(_Target);
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(_Target);
#endif
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private IEnumerable GetValuePresetDropdown()
        {
            return StateControlEditorUtils.GetValuePresetDropdown(Target.GetType());
        }
        
        [UsedImplicitly]
        private void ValidateValuePresets(List<AStateValuePreset> value, ValidationResult result)
        {
            StateControlEditorUtils.ValidateStateValuePresets(Target.GetType(), value, result); 
        }
#endif
    }
}
