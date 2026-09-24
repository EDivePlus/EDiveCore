// Author: Michal Petr
// Created: 23.09.2026

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.ProceduralUI
{
    [Serializable]
    public struct ArcCutout
    {
        private const float FULL_SWEEP = 360f;

        [SerializeField]
        private Vector2 _Anchor;

        [SerializeField]
        private float _MinAngle;

        [SerializeField]
        private float _MaxAngle;

        [Range(0f, 1f)]
        [SerializeField]
        private float _Value;

        [SerializeField]
        private ArcFillOrigin _FillOrigin;

        [SerializeField]
        private float _EdgePadding;

        [MinValue(0f)]
        [SerializeField]
        private float _CornerRadius;

        [SerializeField]
        private bool _SharpCenter;

        public static ArcCutout Default => new()
        {
            _Anchor = new Vector2(0.5f, 0.5f),
            _MaxAngle = FULL_SWEEP,
            _Value = 1f
        };

        public Vector2 Anchor { get => _Anchor; set => _Anchor = value; }
        public float MinAngle { get => _MinAngle; set => _MinAngle = value; }
        public float MaxAngle { get => _MaxAngle; set => _MaxAngle = value; }
        public float Value { get => _Value; set => _Value = Mathf.Clamp01(value); }
        public ArcFillOrigin FillOrigin { get => _FillOrigin; set => _FillOrigin = value; }
        public float EdgePadding { get => _EdgePadding; set => _EdgePadding = value; }
        public float CornerRadius { get => _CornerRadius; set => _CornerRadius = Mathf.Max(0f, value); }
        public bool SharpCenter { get => _SharpCenter; set => _SharpCenter = value; }

        public bool IsFull => FilledSweep >= FULL_SWEEP;
        public bool IsEmpty => FilledSweep <= 0f;
        public float ShaderCornerRadius => _CornerRadius;

        // Rides the top bit of the encoded fill; matches DecodeFill in ProceduralShape.cginc
        public float ShaderSharpCenterFlag => _SharpCenter ? 8388608f : 0f;

        private float Sweep => Mathf.Clamp(_MaxAngle - _MinAngle, 0f, FULL_SWEEP);
        private float FilledSweep => Sweep * Mathf.Clamp01(_Value);

        public void ResolveAngles(out float start, out float end)
        {
            var sweep = Sweep;
            var filled = FilledSweep;
            switch (_FillOrigin)
            {
                case ArcFillOrigin.End:
                    end = _MinAngle + sweep;
                    start = end - filled;
                    break;
                case ArcFillOrigin.Center:
                    var mid = _MinAngle + sweep * 0.5f;
                    start = mid - filled * 0.5f;
                    end = mid + filled * 0.5f;
                    break;
                default:
                    start = _MinAngle;
                    end = start + filled;
                    break;
            }
        }

        // xy = apex in pixels from the rect center, zw = start and end angle in radians; a full sweep disables the cut.
        // Edge padding moves both edges inward (negative moves them outward), which is the same sector with its apex shifted along the bisector.
        public Vector4 ResolveShaderParams(float width, float height)
        {
            if (IsFull)
                return new Vector4(0f, 0f, 0f, Mathf.PI * 2f);

            ResolveAngles(out var start, out var end);
            start *= Mathf.Deg2Rad;
            end *= Mathf.Deg2Rad;

            var apex = new Vector2((_Anchor.x - 0.5f) * width, (_Anchor.y - 0.5f) * height);
            if (!Mathf.Approximately(_EdgePadding, 0f))
            {
                var center = (start + end) * 0.5f;
                var halfSweep = (end - start) * 0.5f;
                var shift = _EdgePadding / Mathf.Max(Mathf.Sin(halfSweep), 0.0001f);
                apex += new Vector2(Mathf.Sin(center), Mathf.Cos(center)) * shift;
            }

            return new Vector4(apex.x, apex.y, start, end);
        }

        // Mirrors SectorDistance in ProceduralShape.cginc without corner rounding; p is in pixels from the rect center
        public bool Contains(Vector2 p, float width, float height)
        {
            if (IsFull)
                return true;
            if (IsEmpty)
                return false;

            var arc = ResolveShaderParams(width, height);
            var q = p - new Vector2(arc.x, arc.y);
            var half = (arc.w - arc.z) * 0.5f;
            var center = (arc.z + arc.w) * 0.5f;
            var up = new Vector2(Mathf.Sin(center), Mathf.Cos(center));
            var local = new Vector2(q.x * up.y - q.y * up.x, Vector2.Dot(q, up));

            var sign = 1f;
            if (half > Mathf.PI * 0.5f)
            {
                local = -local;
                half = Mathf.PI - half;
                sign = -1f;
            }

            var sc = new Vector2(Mathf.Sin(half), Mathf.Cos(half));
            local.x = Mathf.Abs(local.x);
            var m = (local - sc * Mathf.Max(Vector2.Dot(local, sc), 0f)).magnitude;
            var d = m * Mathf.Sign(sc.y * local.x - sc.x * local.y);
            return sign * d <= 0f;
        }
    }
}
