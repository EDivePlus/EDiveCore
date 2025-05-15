using System.Collections;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.StateValuePresets;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
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
            if (_ValuePresets == null) 
                return;
            
            foreach (var valuePreset in _ValuePresets)
            {
                if (valuePreset == null)
                {
                    Debug.LogError("Name: " + _Target.name + ", Value preset is null!");
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
        private void ValidateValuePresets(List<AStateValuePreset> value, SelfValidationResult result)
        {
            if (Target == null)
                return;

            StateControlEditorUtils.ValidateStateValuePresets(Target.GetType(), value, result);
        }
#endif
    }
}
