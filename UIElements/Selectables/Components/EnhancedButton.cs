// Author: František Holubec
// Created: 19.04.2024

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
#endif

// ReSharper disable once CheckNamespace
namespace EDIVE.UIElements.Selectables
{
    public class EnhancedButton : Button, IEnhancedSelectable
    {
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private SelectableAdditionalData _AdditionalData = new();

        public SelectableAdditionalData AdditionalData => _AdditionalData;

        public void RefreshVisuals() => DoStateTransition(currentSelectionState, true);

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
    [UnityEditor.CustomEditor(typeof(EnhancedButton))]
    [UnityEditor.CanEditMultipleObjects]
    public class EnhancedButtonEditor : NativeWrapperOdinEditor<Button, UnityEditor.UI.ButtonEditor> { }
#endif
}
