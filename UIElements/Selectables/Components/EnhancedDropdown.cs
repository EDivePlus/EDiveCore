// Author: František Holubec
// Created: 13.10.2026

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
using TMPro.EditorUtilities;
#endif

// ReSharper disable once CheckNamespace
namespace EDIVE.UIElements.Selectables
{
    public class EnhancedDropdown : TMP_Dropdown, IEnhancedSelectable
    {
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private SelectableAdditionalData _AdditionalData = new();

        private GameObject _list;

        public SelectableAdditionalData AdditionalData => _AdditionalData;

        public void RefreshVisuals() => DoStateTransition(currentSelectionState, true);

        // The open list is a child, hovering it must not highlight the field
        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (_list != null && eventData.pointerEnter != null && eventData.pointerEnter.transform.IsChildOf(_list.transform))
                return;

            base.OnPointerEnter(eventData);
        }

        protected override GameObject CreateDropdownList(GameObject template)
        {
            _list = base.CreateDropdownList(template);
            return _list;
        }

        protected override void DestroyDropdownList(GameObject dropdownList)
        {
            if (dropdownList == _list)
                _list = null;
            base.DestroyDropdownList(dropdownList);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _AdditionalData.DoStateTransition((Selectables.SelectionState) state, instant);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(EnhancedDropdown))]
    [UnityEditor.CanEditMultipleObjects]
    public class EnhancedDropdownEditor : NativeWrapperOdinEditor<TMP_Dropdown, DropdownEditor> { }
#endif
}
