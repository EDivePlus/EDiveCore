// Author: František Holubec
// Created: 13.10.2026

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
#endif

// ReSharper disable once CheckNamespace
namespace EDIVE.UIElements.Selectables
{
    public class EnhancedScrollbar : Scrollbar, IEnhancedSelectable
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
    [UnityEditor.CustomEditor(typeof(EnhancedScrollbar))]
    [UnityEditor.CanEditMultipleObjects]
    public class EnhancedScrollbarEditor : NativeWrapperOdinEditor<Scrollbar, UnityEditor.UI.ScrollbarEditor> { }
#endif
}
