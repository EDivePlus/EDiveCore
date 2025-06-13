// Author: František Holubec
// Created: 11.06.2025

using EDIVE.DataStructures;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Utils.FontSymbols
{
    [AddComponentMenu("UI/Font Symbol (TMP Text UI)")]
    public class FontSymbolTMPTextUI : TextMeshProUGUI
    {
        [SerializeField]
        [OnValueChanged(nameof(RefreshSymbol))]
        [EnhancedValidate("ValidateFontSymbol")]
        private FontSymbol _FontSymbol;

        [SerializeField]
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

        [ShowInInspector]
        public bool IsScaleStatic
        {
            get => isTextObjectScaleStatic;
            set => isTextObjectScaleStatic = value;
        }

        private void RefreshSymbol()
        {
            enableAutoSizing = false;
            material = null;
            richText = false;
            textWrappingMode = TextWrappingModes.NoWrap;
            overflowMode = TextOverflowModes.Overflow;
            alignment = TextAlignmentOptions.Center;
            emojiFallbackSupport = false;
            isOrthographic = true;

            if (!Definition)
                return;
            if (Definition.TMPFont == null)
                return;

            font = Definition.TMPFont;
            text = $"{Symbol}";
            fontSize = Mathf.FloorToInt(Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * _Scale);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            RefreshSymbol();
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateFontSymbol(FontSymbol symbol, SelfValidationResult result, InspectorProperty property)
        {
            if (symbol.Definition == null)
            {
                result.AddError("Missing symbol definition");
                return;
            }

            if (symbol.Definition.TMPFont == null)
            {
                result.AddError("Definition does not have a valid TMP Font assigned");
                return;
            }

            if (symbol.CheckSymbolAvailability() == FontSymbolAvailability.NotSupported)
            {
                result.AddError("Symbol is not supported by the assigned TMP Font.")
                    .WithFix(() => symbol.Definition.TryAddCharacterToTMP(symbol.Symbol));
            }
        }

        protected override void Reset()
        {
            base.Reset();
            Symbol = '\uef55';
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
    [CustomEditor(typeof(FontSymbolTMPTextUI))]
    [CanEditMultipleObjects]
    public class FontSymbolTMPTextUIEditor : OdinEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            foreach (var property in Tree.RootProperty.Children)
            {
                if (!typeof(FontSymbolTMPTextUI).IsAssignableFrom(property.Info.TypeOfOwner))
                    property.State.Visible = false;
            }
        }
    }
#endif
}
