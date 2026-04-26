// Author: František Holubec
// Created: 17.02.2026

using System;
using System.Collections;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.VisualPresets.StateHandling
{
    [Serializable]
    public class MultiStateVisualPresetRecord : AVisualPresetRecord<MultiStateVisualID>
    {
        [VerticalGroup("Value")]
        [EnhancedValidate("ValidateState")]
        [EnhancedValueDropdown("GetStatesDropdown")]
        [InlineIconButton("Pen", "OpenStatesEdit", EnableIf = "@VisualID != null")]
        [SerializeField]
        private string _State;
        
        public string State
        {
            get => _State;
            set => _State = value;
        }
        
        public override string EditorLabel => "MultiState";
        
        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is MultiStateVisualPresetRecord multiStatePreset && Equals(multiStatePreset.State, State);
        }

        public override int GetHashCodeInternal()
        {
            return State.GetHashCode();
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private IEnumerable GetStatesDropdown()
        {
            return VisualID != null ? VisualID.AvailableStates : null;
        }
        
        [UsedImplicitly]
        private void ValidateState(string value, SelfValidationResult result)
        {
            if (value == null || VisualID == null)
                return;

            if (!VisualID.HasState(value))
            {
                result.AddError("State is not available!");
            }
        }
        
        [UsedImplicitly]
        private void OpenStatesEdit(Rect fieldRect, InspectorProperty property)
        {
            if (VisualID == null) 
                return;
            
            if (Event.current.button == 0)
            {
                EditorApplication.delayCall += () => GUIHelper.OpenInspectorWindow(VisualID);
            }
            else if (Event.current.button == 1)
                OdinEditorWindow.InspectObjectInDropDown(VisualID, fieldRect, 400);
        }
#endif
    }
}
