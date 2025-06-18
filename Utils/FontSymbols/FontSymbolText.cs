// Author: František Holubec
// Created: 11.06.2025

using EDIVE.DataStructures;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Utils.FontSymbols
{
    [AddComponentMenu("UI/Font Symbol (Text)")]
    public class FontSymbolText : Text
    {
        [SerializeField]
        [OnValueChanged(nameof(RefreshSymbol))]
        private FontSymbol _FontSymbol;

        [SerializeField]
        [OnValueChanged(nameof(RefreshSymbol))]
        private float _Scale = 1f;

        public FontSymbol FontSymbol
        {
            get => _FontSymbol;
            set
            {
                _FontSymbol = value;
                RefreshSymbol();
            }
        }

        public char Symbol
        {
            get => FontSymbol.Symbol;
            set => FontSymbol = FontSymbol.WithSymbol(value);
        }

        public FontSymbolsDefinition Definition
        {
            get => FontSymbol.Definition;
            set => FontSymbol = FontSymbol.WithDefinition(value);
        }

        public float Scale
        {
            get => _Scale;
            set
            {
                _Scale = value;
                RefreshSymbol();
            }
        }

        [PropertySpace]
        [ShowInInspector]
        public Color Color
        {
            get => color;
            set => color = value;
        }

        [ShowInInspector]
        public Material Material
        {
            get => material;
            set => material = value;
        }

        [ShowInInspector]
        public bool RaycastTarget
        {
            get => raycastTarget;
            set => raycastTarget = value;
        }

        [ShowInInspector]
        public RaycastPadding RaycastPadding
        {
            get => raycastPadding;
            set => raycastPadding = value;
        }

        [ShowInInspector]
        public bool Maskable
        {
            get => maskable;
            set => maskable = value;
        }

        private void RefreshSymbol()
        {
            var changed = false;
            material = null;
            alignment = TextAnchor.MiddleCenter;
            supportRichText = false;
            horizontalOverflow = HorizontalWrapMode.Overflow;
            verticalOverflow = VerticalWrapMode.Overflow;

            if (!Definition)
                return;

            if (font != Definition.Font)
            {
                font = Definition.Font;
                changed = true;
            }

            var newFontSize = Mathf.FloorToInt(Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * _Scale);
            if (newFontSize > 0 && fontSize != newFontSize)
            {
                fontSize = newFontSize;
                changed = true;
            }

            var newText = $"{Symbol}";
            if (text != newText)
            {
                text = newText;
                changed = true;
            }

#if UNITY_EDITOR
            if (changed) EditorUtility.SetDirty(this);
#endif
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            RefreshSymbol();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            Symbol = '\uef55';
            RefreshSymbol();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            RefreshSymbol();
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FontSymbolText))]
    [CanEditMultipleObjects]
    public class FontSymbolTextEditor : ABaseFontSymbolEditor<FontSymbolText> { }
#endif
}
