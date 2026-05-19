// Author: Michal Petr
// Created: 19.05.2026

using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EDIVE.UIElements
{
    [RequireComponent(typeof(LayoutElement))]
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteAlways]
    public class TextMaxWidth : MonoBehaviour, ILayoutElement
    {
        [SerializeField]
        private float _MaxWidth = 400f;

        private TMP_Text _text;
        private LayoutElement _layoutElement;

        private void EnsureReferences()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (_layoutElement == null) _layoutElement = GetComponent<LayoutElement>();
        }

        private void OnEnable()
        {
            EnsureReferences();
            SetDirty();
        }

        private void OnDisable()
        {
            SetDirty();
        }

        // Driven by the layout system instead of TMP_Text.OnPreRenderText, so the
        // width is resolved during the layout pass (before rendering) on frame 1,
        // and recomputed automatically whenever TMP marks its layout dirty.
        public void CalculateLayoutInputHorizontal()
        {
            EnsureReferences();
            if (_text == null || _layoutElement == null) return;

            var preferred = _text.GetPreferredValues(_text.text, Mathf.Infinity, Mathf.Infinity).x;

            if (preferred > _MaxWidth)
            {
                _layoutElement.preferredWidth = _MaxWidth;
                _text.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                _layoutElement.preferredWidth = preferred;
            }
        }

        public void CalculateLayoutInputVertical() { }

        // This component only acts as a calculation hook; the actual preferred
        // width is exposed through the sibling LayoutElement, so report nothing
        // here (negative values are ignored by LayoutUtility).
        public float minWidth => -1;
        public float preferredWidth => -1;
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => -1;
        public float flexibleHeight => -1;
        public int layoutPriority => 0;

        private void SetDirty()
        {
            if (!isActiveAndEnabled) return;
            if (transform is RectTransform rectTransform)
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        private void OnValidate()
        {
            SetDirty();
        }
    }
}
