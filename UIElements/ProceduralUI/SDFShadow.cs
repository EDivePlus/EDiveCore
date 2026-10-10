// Author: František Holubec
// Created: 09.10.2026

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.ProceduralUI
{
    public class SDFShadow : ASDFEffect
    {
        [EnumToggleButtons]
        [SerializeField]
        private ShadowMode _Mode;

        [MinValue(0f)]
        [SerializeField]
        private float _Size;

        [MinValue(0f)]
        [SerializeField]
        private float _Blur = 10f;

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
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_Size, value)) return; _Size = value; SetDirty(); }
        }

        public float Blur
        {
            get => _Blur;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_Blur, value)) return; _Blur = value; SetDirty(); }
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

        private float SizeRounded => Mathf.Round(VertexPacking.ClampPixel(_Size));
        private float BlurRounded => Mathf.Round(VertexPacking.ClampPixel(_Blur));
        private float OffsetReach => Mathf.Max(Mathf.Abs(VertexPacking.QuantizeOffset(_Offset.x)), Mathf.Abs(VertexPacking.QuantizeOffset(_Offset.y)));

        internal override SDFLayer Layer => _Mode == ShadowMode.Inner ? SDFLayer.InnerShadow : SDFLayer.OuterShadow;

        internal override Color LayerColor => _Color;

        // Starts past the outlines: their outer edge for outer, their inner edge for inner
        internal override float GetReach(in SDFEffectContext context) =>
            _Mode == ShadowMode.Inner ? 0f : context.OutlineReach + SizeRounded + BlurRounded + OffsetReach;

        // Inner shadow fades out size past its start, the offset can push it further in
        internal override float GetHoleInset(in SDFEffectContext context) =>
            _Mode == ShadowMode.Inner ? context.OutlineInnerReach + SizeRounded + OffsetReach + HOLE_MARGIN : -1f;

        // size + blur * 4096, offset, power + start * 256
        internal override Vector3 PackData(in SDFEffectContext context)
        {
            var start = _Mode == ShadowMode.Inner ? context.OutlineInnerReach : context.OutlineReach;
            return new Vector3(SizeRounded + BlurRounded * 4096f, VertexPacking.PackOffsets(_Offset), VertexPacking.ClampShadowPower(_Power) + start * 256f);
        }
    }
}
