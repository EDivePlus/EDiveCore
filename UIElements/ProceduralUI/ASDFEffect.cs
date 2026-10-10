// Author: František Holubec
// Created: 09.10.2026

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.ProceduralUI
{
    // Extra layer drawn by the SDFGraphic on the same object, as its own quad in the same mesh
    [ExecuteAlways]
    [RequireComponent(typeof(SDFGraphic))]
    public abstract class ASDFEffect : MonoBehaviour
    {
        [PropertyOrder(10)]
        [Tooltip("Fade with the Graphic color alpha")]
        [SerializeField]
        private bool _UseGraphicAlpha = true;

        private SDFGraphic _graphic;

        public SDFGraphic Graphic => _graphic ? _graphic : _graphic = GetComponent<SDFGraphic>();

        public bool UseGraphicAlpha
        {
            get => _UseGraphicAlpha;
            set { if (_UseGraphicAlpha == value) return; _UseGraphicAlpha = value; SetDirty(); }
        }

        internal abstract SDFLayer Layer { get; }
        internal abstract Color LayerColor { get; }

        // How far the layer reaches past the shape edge, in px
        internal abstract float GetReach(in SDFEffectContext context);

        // uv2.yzw, see ProceduralSimpleSDF.shader
        internal abstract Vector3 PackData(in SDFEffectContext context);

        // How far in from the edge the layer stays empty, past the corner radius; negative when it fills the middle
        internal virtual float GetHoleInset(in SDFEffectContext context) => -1f;

        // Room for the antialiasing next to the hole
        protected const float HOLE_MARGIN = 2f;

        protected virtual void OnEnable() => SetComponentsDirty();
        protected virtual void OnDisable() => SetComponentsDirty();
        protected virtual void OnDestroy() => SetComponentsDirty();
        protected virtual void OnValidate() => SetDirty();
        protected virtual void OnDidApplyAnimationProperties() => SetDirty();

        protected void SetDirty()
        {
            if (Graphic)
                Graphic.SetVerticesDirty();
        }

        private void SetComponentsDirty()
        {
            if (Graphic)
                Graphic.SetComponentsDirty();
        }
    }

    // Matches the LAYER_ defines in ProceduralSimpleSDF.shader
    internal enum SDFLayer
    {
        Fill = 0,
        OuterShadow = 1,
        InnerShadow = 2,
        Outline = 3,
        SolidFill = 4
    }

    internal readonly struct SDFEffectContext
    {
        public readonly float OutlineReach;
        public readonly float OutlineInnerReach;
        public readonly float FillInset;
        public readonly bool HasVisibleFill;

        public SDFEffectContext(float outlineReach, float outlineInnerReach, float fillInset, bool hasVisibleFill)
        {
            OutlineReach = outlineReach;
            OutlineInnerReach = outlineInnerReach;
            FillInset = fillInset;
            HasVisibleFill = hasVisibleFill;
        }
    }
}
