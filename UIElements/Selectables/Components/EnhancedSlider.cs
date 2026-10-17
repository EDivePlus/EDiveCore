// Author: František Holubec
// Created: 13.10.2026

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
#endif

// ReSharper disable once CheckNamespace
namespace EDIVE.UIElements.Selectables
{
    public class EnhancedSlider : Slider, IEnhancedSelectable
    {
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private SelectableAdditionalData _AdditionalData = new();

        // Optional text showing the value, e.g. 80 %
        [SerializeField]
        [FoldoutGroup("Value Text")]
        private TMP_Text _ValueText;

        // Value 0 to 1 between min and max instead of the raw value
        [SerializeField]
        [FoldoutGroup("Value Text")]
        private bool _NormalizedValueText = true;

        [SerializeField]
        [FoldoutGroup("Value Text")]
        private string _ValueTextFormat = "{0:0%}";

        public SelectableAdditionalData AdditionalData => _AdditionalData;

        public void RefreshVisuals() => DoStateTransition(currentSelectionState, true);

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshValueText();
        }

        protected override void Set(float input, bool sendCallback = true)
        {
            base.Set(input, sendCallback);
            RefreshValueText();
        }

        public void RefreshValueText()
        {
            if (_ValueText != null)
                _ValueText.text = string.Format(_ValueTextFormat, _NormalizedValueText ? normalizedValue : value);
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
    [UnityEditor.CustomEditor(typeof(EnhancedSlider))]
    [UnityEditor.CanEditMultipleObjects]
    public class EnhancedSliderEditor : NativeWrapperOdinEditor<Slider, UnityEditor.UI.SliderEditor> { }
#endif
}
