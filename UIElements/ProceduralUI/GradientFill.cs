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
        private Color _Color;

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
            _Color = Color.white,
            _GradientColor = Color.white,
            _GradientQuality = 8,
            _RadialSize = 1f
        };

        public Color Color { get => _Color; set => _Color = value; }
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

        // The vertex carries the tinted fill color with the tint alpha alone; fill alpha travels in the shader data
        public Color GetVertexColor(Color tint) => new(_Color.r * tint.r, _Color.g * tint.g, _Color.b * tint.b, tint.a);

        public Color GetGradientColor(Color tint)
        {
            var c = _GradientType == GradientType.None ? _Color : _GradientColor;
            return new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a);
        }

        // Matches DecodeFill in ProceduralShape.cginc: radialSize * 100 + mode * 4096 + fillAlpha * 32768
        public float EncodeShaderFill()
        {
            if (_GradientType == GradientType.Gradient)
                return 255f * 32768f;

            var mode = _GradientType switch
            {
                GradientType.Vertical => 1,
                GradientType.Horizontal => 2,
                GradientType.Radial => 3 + (int) _RadialMode,
                _ => 0
            };
            var size = Mathf.Round(VertexPacking.ClampRadialSize(RadialSize) * 100f);
            Color32 fill = _Color;
            return size + mode * 4096f + fill.a * 32768f;
        }

        public Color Evaluate(float fx, float fy, float width, float height, Color tint)
        {
            switch (_GradientType)
            {
                case GradientType.Radial:
                {
                    float t;
                    if (_RadialMode == RadialMode.Ellipse)
                    {
                        var dx = fx - 0.5f;
                        var dy = fy - 0.5f;
                        t = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    }
                    else
                    {
                        var refDim = _RadialMode == RadialMode.CircleCover ? Mathf.Max(width, height) : Mathf.Min(width, height);
                        var dx = (fx - 0.5f) * width;
                        var dy = (fy - 0.5f) * height;
                        t = Mathf.Sqrt(dx * dx + dy * dy) / (refDim * 0.5f);
                    }
                    return Color.Lerp(_Color * tint, _GradientColor * tint, Mathf.Clamp01(t / RadialSize));
                }
                case GradientType.Gradient:
                {
                    var angle = _GradientAngle * Mathf.Deg2Rad;
                    var t = Mathf.Clamp01((fx - 0.5f) * Mathf.Cos(angle) + (fy - 0.5f) * Mathf.Sin(angle) + 0.5f);
                    return (_Gradient?.Evaluate(t) ?? Color.white) * tint;
                }
                default:
                    return _Color * tint;
            }
        }
    }
}
