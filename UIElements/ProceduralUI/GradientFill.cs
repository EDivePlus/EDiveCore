// Author: Michal Petr
// Created: 22.09.2026

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.ProceduralUI
{
    [Serializable]
    public struct GradientFill
    {
        public const int MIN_QUALITY = 2;
        public const int MAX_QUALITY = 20;
        private const float MIN_RADIAL_SIZE = 0.01f;

        [SerializeField]
        private GradientType _GradientType;

        [SerializeField]
        [ShowIf(nameof(ShowGradientColor))]
        private Color _GradientColor;

        [SerializeField]
        [ShowIf(nameof(RequiresSubdivision))]
        [Range(MIN_QUALITY, MAX_QUALITY)]
        private int _GradientQuality;

        [SerializeField]
        [ShowIf(nameof(IsRadial))]
        private RadialMode _RadialMode;

        [SerializeField]
        [ShowIf(nameof(IsRadial))]
        [MinValue(MIN_RADIAL_SIZE)]
        private float _RadialSize;

        [SerializeField]
        [ShowIf(nameof(IsGradient))]
        private Gradient _Gradient;

        [SerializeField]
        [ShowIf(nameof(IsGradient))]
        [Range(0f, 360f)]
        private float _GradientAngle;

        public static GradientFill Default => new()
        {
            _GradientColor = Color.white,
            _GradientQuality = 8,
            _RadialSize = 1f
        };

        public GradientType GradientType { get => _GradientType; set => _GradientType = value; }
        public Color GradientColor { get => _GradientColor; set => _GradientColor = value; }
        public int GradientQuality { get => Mathf.Clamp(_GradientQuality, MIN_QUALITY, MAX_QUALITY); set => _GradientQuality = Mathf.Clamp(value, MIN_QUALITY, MAX_QUALITY); }
        public RadialMode RadialMode { get => _RadialMode; set => _RadialMode = value; }
        public float RadialSize { get => Mathf.Max(MIN_RADIAL_SIZE, _RadialSize); set => _RadialSize = Mathf.Max(MIN_RADIAL_SIZE, value); }
        public Gradient Gradient { get => _Gradient; set => _Gradient = value; }
        public float GradientAngle { get => _GradientAngle; set => _GradientAngle = value; }

        // Only a keyed gradient is sampled per vertex; every other type is evaluated in the shader
        public bool RequiresSubdivision => _GradientType == GradientType.Gradient;
        private bool IsRadial => _GradientType == GradientType.Radial;
        private bool IsGradient => _GradientType == GradientType.Gradient;
        private bool ShowGradientColor => _GradientType is GradientType.Vertical or GradientType.Horizontal or GradientType.Radial;

        // Matches DecodeFill in ProceduralShape.cginc: radialSize * 100 + mode * 4096.
        // The fill color is Graphic.color in the vertex; a keyed gradient is baked into the vertex colors instead.
        public float EncodeShaderFill()
        {
            if (_GradientType == GradientType.Gradient)
                return 0f;

            var mode = _GradientType switch
            {
                GradientType.Vertical => 1,
                GradientType.Horizontal => 2,
                GradientType.Radial => 3 + (int) _RadialMode,
                _ => 0
            };
            var size = Mathf.Round(VertexPacking.ClampRadialSize(RadialSize) * 100f);
            return size + mode * 4096f;
        }

        // Vertex color of a subdivided keyed gradient; Graphic.color multiplies the keys
        public Color Evaluate(float fx, float fy, Color color)
        {
            var angle = _GradientAngle * Mathf.Deg2Rad;
            var t = Mathf.Clamp01((fx - 0.5f) * Mathf.Cos(angle) + (fy - 0.5f) * Mathf.Sin(angle) + 0.5f);
            return (_Gradient?.Evaluate(t) ?? Color.white) * color;
        }
    }
}
