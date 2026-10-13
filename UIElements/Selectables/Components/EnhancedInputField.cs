// Author: František Holubec
// Created: 13.10.2026

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
using TMPro.EditorUtilities;
#endif

// ReSharper disable once CheckNamespace
namespace EDIVE.UIElements.Selectables
{
    public class EnhancedInputField : TMP_InputField, IEnhancedSelectable
    {
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private SelectableAdditionalData _AdditionalData = new();

        public SelectableAdditionalData AdditionalData => _AdditionalData;

        public void RefreshVisuals() => DoStateTransition(currentSelectionState, true);

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _AdditionalData.DoStateTransition((Selectables.SelectionState) state, instant);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(EnhancedInputField))]
    [UnityEditor.CanEditMultipleObjects]
    public class EnhancedInputFieldEditor : NativeWrapperOdinEditor<TMP_InputField, TMP_InputFieldEditor> { }
#endif
}
