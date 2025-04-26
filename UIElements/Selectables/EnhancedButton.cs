// Author: František Holubec
// Created: 19.04.2024

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.Selectables
{
    public class EnhancedButton : Button
    {
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private SelectableAdditionalData _AdditionalData = new();

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _AdditionalData.DoStateTransition((Selectables.SelectionState) state, instant);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(EnhancedButton))]
    [UnityEditor.CanEditMultipleObjects]
    public class EnhancedButtonEditor : EnhancedSelectableEditor<Button, UnityEditor.UI.ButtonEditor> { }
#endif
}
