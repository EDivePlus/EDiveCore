// Author: František Holubec
// Created: 09.10.2026

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.ProceduralUI
{
    public class SDFOutline : ASDFEffect
    {
        [MinValue(0f)]
        [SerializeField]
        private float _Size = 2f;

        [SerializeField]
        private Color _Color = Color.black;

        [SerializeField]
        private EdgePlacement _Placement = EdgePlacement.Outside;

        [MinValue(0f)]
        [Tooltip("Overlap with the fill to hide the seam. 0 = off")]
        [SerializeField]
        private float _SeamOverlap = 1f;

        public float Size
        {
            get => _Size;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_Size, value)) return; _Size = value; SetDirty(); }
        }

        public Color Color
        {
            get => _Color;
            set { if (_Color == value) return; _Color = value; SetDirty(); }
        }

        public EdgePlacement Placement
        {
            get => _Placement;
            set { if (_Placement == value) return; _Placement = value; SetDirty(); }
        }

        public float SeamOverlap
        {
            get => _SeamOverlap;
            set { value = Mathf.Clamp(value, 0f, VertexPacking.MAX_FILL_INSET); if (Mathf.Approximately(_SeamOverlap, value)) return; _SeamOverlap = value; SetDirty(); }
        }

        private float ClampedSize => VertexPacking.ClampPixel(_Size);
        private float ClampedSeamOverlap => Mathf.Clamp(_SeamOverlap, 0f, VertexPacking.MAX_FILL_INSET);

        internal float OuterReach => _Placement.OuterExtent(ClampedSize);
        internal float InnerReach => ClampedSize - OuterReach;

        // Outside outlines reach under the fill
        internal float GetFillOverlap(bool hasVisibleFill) => hasVisibleFill && _Placement == EdgePlacement.Outside ? ClampedSeamOverlap : 0f;

        // Inside and center outlines pull the fill edge in under them
        internal float FillInset => _Placement == EdgePlacement.Outside ? 0f : Mathf.Min(ClampedSeamOverlap, InnerReach);

        internal override SDFLayer Layer => SDFLayer.Outline;
        internal override Color LayerColor => _Color;
        internal override float GetReach(in SDFEffectContext context) => OuterReach;
        internal override float GetHoleInset(in SDFEffectContext context) => InnerReach + GetFillOverlap(context.HasVisibleFill) + HOLE_MARGIN;

        // size + placement * 4096, overlap under the fill
        internal override Vector3 PackData(in SDFEffectContext context) =>
            new(ClampedSize + (int) _Placement * 4096f, GetFillOverlap(context.HasVisibleFill), 0f);
    }
}
