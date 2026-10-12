// Author: František Holubec
// Created: 09.10.2026

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.SDFShapes
{
    public class SDFShadow : ASDFEffect
    {
        // Matches SHADOW_FILL_OVERLAP in SDFShape.shader, covers the fill antialiasing
        private const float FILL_OVERLAP = 1f;

        [EnumToggleButtons]
        [SerializeField]
        private ShadowMode _Mode;

        [MinValue(0f)]
        [SerializeField]
        private float _Size;

        [MinValue(0f)]
        [SerializeField]
        private float _Blur = 10f;

        [Tooltip("1 = linear, higher = tighter")]
        [MinValue(VertexPacking.MIN_SHADOW_POWER)]
        [SerializeField]
        private float _Power = 1f;

        [SerializeField]
        private Vector2 _Offset;

        [SerializeField]
        private Color _Color = Color.black;

        public ShadowMode Mode
        {
            get => _Mode;
            set { if (_Mode == value) return; _Mode = value; SetDirty(); }
        }

        public float Size
        {
            get => _Size;
            set { value = VertexPacking.ClampShadowPixel(value); if (Mathf.Approximately(_Size, value)) return; _Size = value; SetDirty(); }
        }

        public float Blur
        {
            get => _Blur;
            set { value = VertexPacking.ClampShadowPixel(value); if (Mathf.Approximately(_Blur, value)) return; _Blur = value; SetDirty(); }
        }

        public float Power
        {
            get => _Power;
            set { value = VertexPacking.ClampShadowPower(value); if (Mathf.Approximately(_Power, value)) return; _Power = value; SetDirty(); }
        }

        public Vector2 Offset
        {
            get => _Offset;
            set { if (_Offset == value) return; _Offset = value; SetDirty(); }
        }

        public Color Color
        {
            get => _Color;
            set { if (_Color == value) return; _Color = value; SetDirty(); }
        }

        private float SizeQuantized => VertexPacking.QuantizeShadowPixel(_Size);
        private float BlurQuantized => VertexPacking.QuantizeShadowPixel(_Blur);
        private float OffsetReach => Mathf.Max(Mathf.Abs(VertexPacking.QuantizeOffset(_Offset.x)), Mathf.Abs(VertexPacking.QuantizeOffset(_Offset.y)));

        internal override SDFLayer Layer => _Mode == ShadowMode.Inner ? SDFLayer.InnerShadow : SDFLayer.OuterShadow;

        internal override Color LayerColor => _Color;

        // Starts past the outlines: their outer edge for outer, their inner edge for inner
        internal override float GetReach(in SDFEffectContext context) =>
            _Mode == ShadowMode.Inner ? 0f : context.OutlineReach + SizeQuantized + BlurQuantized + OffsetReach;

        // Inner shadow fades out size past its start, the offset can push it further in.
        // Outer shadow under an opaque fill is cut off there, so its middle is empty.
        internal override float GetHoleInset(in SDFEffectContext context)
        {
            if (_Mode == ShadowMode.Inner)
                return context.OutlineInnerReach + SizeQuantized + OffsetReach + HOLE_MARGIN;
            return HidesUnderFill(context) ? FILL_OVERLAP + HOLE_MARGIN : -1f;
        }

        // On a frame the outer shadow wraps the inner edge too, the inner shadow stays in the frame
        internal override float GetFrameHoleInset(in SDFEffectContext context) =>
            _Mode == ShadowMode.Inner ? HOLE_MARGIN : context.OutlineReach + SizeQuantized + BlurQuantized + OffsetReach + HOLE_MARGIN;

        // size * 4 + blur * 4 * 4096, offset, power + start * 256; negative when hidden under the fill
        internal override Vector3 PackData(in SDFEffectContext context)
        {
            var start = _Mode == ShadowMode.Inner ? context.OutlineInnerReach : context.OutlineReach;
            var powerStart = VertexPacking.ClampShadowPower(_Power) + start * 256f;
            return new Vector3(SizeQuantized * 4f + BlurQuantized * 16384f, VertexPacking.PackOffsets(_Offset), HidesUnderFill(context) ? -powerStart : powerStart);
        }

        // Faded panels show nothing under the fill, like a group fade. Fill edges pulled in further keep the shadow.
        private bool HidesUnderFill(in SDFEffectContext context) =>
            _Mode == ShadowMode.Outer && context.HasOpaqueFill && context.FillInset <= FILL_OVERLAP;
    }
}
