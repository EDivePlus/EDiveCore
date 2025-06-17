// Author: František Holubec
// Created: 11.06.2025

using EDIVE.DataStructures;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
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
            material = null;
            alignment = TextAnchor.MiddleCenter;
            supportRichText = false;
            horizontalOverflow = HorizontalWrapMode.Overflow;
            verticalOverflow = VerticalWrapMode.Overflow;

            if (Definition)
                font = Definition.Font;
            text = $"{Symbol}";
            fontSize = Mathf.FloorToInt(Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * _Scale);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
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

        private void OnDrawGizmosSelected()
        {
            var gui = rectTransform;
            var space = transform;
            var rect = gui.rect;
            var offset = raycastPadding;

            var p0 = space.TransformPoint(new Vector2(rect.x + offset.x, rect.y + offset.y));
            var p1 = space.TransformPoint(new Vector2(rect.x + offset.x, rect.yMax - offset.w));
            var p2 = space.TransformPoint(new Vector2(rect.xMax - offset.z, rect.yMax - offset.w));
            var p3 = space.TransformPoint(new Vector2(rect.xMax - offset.z, rect.y + offset.y));

            Handles.color = Handles.UIColliderHandleColor;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p1, p2);
            Handles.DrawLine(p2, p3);
            Handles.DrawLine(p3, p0);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FontSymbolText))]
    [CanEditMultipleObjects]
    public class FontSymbolTextEditor : ABaseFontSymbolEditor<FontSymbolText> { }
#endif
}
