// Author: Michal Petr
// Created: 22.09.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.SDFShapes
{
    // Colors come from the gradient stops, Graphic.color tints them
    [Serializable]
    public struct GradientFill
    {
        private const float MIN_RADIAL_SIZE = 0.01f;

        [SerializeField]
        [IconEnumToggleButtons]
        private PaintType _Paint;

        [SerializeField]
        [ShowIf(nameof(IsGradient))]
        private Gradient _Gradient;

        [SerializeField]
        [ShowIf(nameof(HasAngle))]
        [Range(0f, 360f)]
        private float _GradientAngle;

        // Offset from the middle in rect sizes, so older data stays centered
        [SerializeField]
        [ShowIf(nameof(HasCenter))]
        [Tooltip("Offset from the middle, in rect sizes")]
        private Vector2 _GradientCenter;

        [SerializeField]
        [ShowIf(nameof(IsRadial))]
        private RadialMode _RadialMode;

        [SerializeField]
        [ShowIf(nameof(IsRadial))]
        [MinValue(MIN_RADIAL_SIZE)]
        private float _RadialSize;

        // Old two color layout, converted once by SDFGraphic
        [SerializeField]
        [HideInInspector]
        private int _GradientType;

        [SerializeField]
        [HideInInspector]
        private Color _GradientColor;

        public static GradientFill Default => new() { _RadialSize = 1f };

        public PaintType Paint { get => _Paint; set => _Paint = value; }
        public Gradient Gradient { get => _Gradient; set => _Gradient = value; }
        public float GradientAngle { get => _GradientAngle; set => _GradientAngle = value; }
        public Vector2 GradientCenter { get => _GradientCenter; set => _GradientCenter = value; }
        public RadialMode RadialMode { get => _RadialMode; set => _RadialMode = value; }
        public float RadialSize { get => Mathf.Max(MIN_RADIAL_SIZE, _RadialSize); set => _RadialSize = Mathf.Max(MIN_RADIAL_SIZE, value); }

        public bool HasGradient => _Paint != PaintType.Solid && _Gradient != null;
        private bool IsGradient => _Paint != PaintType.Solid;
        private bool IsRadial => _Paint == PaintType.Radial;
        private bool HasAngle => _Paint is PaintType.Linear or PaintType.Angular;
        private bool HasCenter => _Paint is PaintType.Radial or PaintType.Angular;

        // Linear runs left to right, angular starts at the top; the angle turns them counterclockwise
        private float ShaderAngle => _Paint == PaintType.Angular ? 90f + _GradientAngle : _GradientAngle;

        public bool IsOpaque
        {
            get
            {
                if (!HasGradient)
                    return true;
                foreach (var key in _Gradient.alphaKeys)
                {
                    if (key.alpha < 1f)
                        return false;
                }
                return true;
            }
        }

        // Matches DecodeFill in SDFShape.cginc: param + mode * 4096.
        // The param is the radial size * 100 for radial, the angle in 1/4096 turns otherwise.
        public float EncodeShaderFill()
        {
            if (!HasGradient)
                return 0f;

            var mode = _Paint switch
            {
                PaintType.Linear => 1,
                PaintType.Radial => 2 + (int) _RadialMode,
                _ => 5
            };
            var param = IsRadial
                ? Mathf.Round(VertexPacking.ClampRadialSize(RadialSize) * 100f)
                : Mathf.Round(Mathf.Repeat(ShaderAngle / 360f, 1f) * 4096f) % 4096f;
            return param + mode * 4096f;
        }

        // Matches DecodeFillExtra in SDFShape.cginc: 12 bits per axis, offset -1 to 1 in 1/2048 steps
        public Vector2 EncodeShaderCenter()
        {
            var offset = HasGradient && HasCenter ? _GradientCenter : Vector2.zero;
            return new Vector2(EncodeCenterAxis(offset.x), EncodeCenterAxis(offset.y));
        }

        private static float EncodeCenterAxis(float offset) => Mathf.Round((Mathf.Clamp(offset, -1f, 1f) + 1f) * 0.5f * 4095f);

        // Old gradients took the start color from Graphic.color: it moves into the stops and the color becomes the tint.
        // Returns whether the color changed.
        internal bool TryMigrateLegacy(ref Color graphicColor)
        {
            if (_GradientType == 0)
                return false;

            var legacyType = _GradientType;
            _GradientType = 0;
            _Paint = legacyType switch
            {
                3 => PaintType.Radial,
                5 => PaintType.Angular,
                _ => PaintType.Linear
            };

            // Keyed gradients already used the stops, tinted by Graphic.color
            if (legacyType == 4)
                return false;

            if (legacyType == 1)
                _GradientAngle = Mathf.Repeat(_GradientAngle + 270f, 360f);

            _Gradient = new Gradient();
            _Gradient.SetKeys(
                new[] { new GradientColorKey(graphicColor, 0f), new GradientColorKey(_GradientColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(_GradientColor.a, 1f) });
            graphicColor = new Color(1f, 1f, 1f, graphicColor.a);
            return true;
        }
    }
}
