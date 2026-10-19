// Author: František Holubec
// Created: 19.04.2024

using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
#endif

// ReSharper disable once CheckNamespace
namespace EDIVE.UIElements.Selectables
{
    public class EnhancedToggle : Toggle, IEnhancedSelectable
    {
        [SerializeField]
        private AToggleState _StateToggle;

        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private SelectableAdditionalData _AdditionalData = new();

        public SelectableAdditionalData AdditionalData => _AdditionalData;

        public void RefreshVisuals()
        {
            if (_StateToggle)
                _StateToggle.RefreshVisuals();
            DoStateTransition(currentSelectionState, true);
        }

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

        // SetIsOnWithoutNotify skips onValueChanged and can't be overridden, callers hold plain Toggle refs
        private void LateUpdate()
        {
            if (_StateToggle && _StateToggle.State != isOn)
                _StateToggle.SetState(isOn);
        }

        private void OnValueChanged(bool newState)
        {
            _StateToggle.SetState(isOn);
        }

        // Transitions started right before destruction would tween destroyed targets
        protected override void OnDestroy()
        {
            _AdditionalData.Kill();
            base.OnDestroy();
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
