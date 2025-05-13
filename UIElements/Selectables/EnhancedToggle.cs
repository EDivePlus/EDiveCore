// Author: František Holubec
// Created: 19.04.2024

using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
#endif

namespace EDIVE.UIElements.Selectables
{
    public class EnhancedToggle : Toggle
    {
        [SerializeField]
        private AToggleState _StateToggle;

        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private SelectableAdditionalData _AdditionalData = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_StateToggle)
            {
                _StateToggle.SetState(isOn, true);
                onValueChanged.AddListener(OnValueChanged);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(bool newState)
        {
            _StateToggle.SetState(isOn);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _AdditionalData.DoStateTransition((Selectables.SelectionState) state, instant);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(EnhancedToggle))]
    [UnityEditor.CanEditMultipleObjects]
    public class EnhancedToggleEditor : NativeWrapperOdinEditor<Toggle, UnityEditor.UI.ToggleEditor> { }
#endif
}
