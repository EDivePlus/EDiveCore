// Author: František Holubec
// Created: 14.10.2026

using System;
using EDIVE.NativeUtils;
using EDIVE.UIElements.SDFShapes;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.View.EditorPreview
{
    [Serializable]
    public class ShapePreviewPlaceholder : APreviewPlaceholder
    {
        [SerializeField]
        private Color _Color = new(0.06f, 0.09f, 0.16f, 0.9f);

        [MinValue(0)]
        [SerializeField]
        private float _CornerRadius;

        public override string Title => "Shape";

        public ShapePreviewPlaceholder() { }
        public ShapePreviewPlaceholder(PreviewEdge edge, float length, float thickness, float margin, Color color, float cornerRadius) : base(edge, length, thickness, margin)
        {
            _Color = color;
            _CornerRadius = cornerRadius;
        }

        public override void Build(RectTransform rect)
        {
            var graphic = rect.gameObject.AddComponent<SDFGraphic>();
            graphic.color = _Color;
            graphic.Roundness = CornerRoundness.Uniform(_CornerRadius);
            graphic.raycastTarget = false;
        }
    }

    // E.g. the real taskbar once it is its own prefab
    [Serializable]
    public class PrefabPreviewPlaceholder : APreviewPlaceholder
    {
        [AssetsOnly]
        [SerializeField]
        private GameObject _Prefab;

        public override string Title => _Prefab != null ? _Prefab.name : "Prefab";

        public override void Build(RectTransform rect)
        {
            if (_Prefab == null)
                return;

            var instance = Object.Instantiate(_Prefab, rect, false);
            if (instance.transform is RectTransform instanceRect)
                instanceRect.SetToFillParent();
        }
    }
}
