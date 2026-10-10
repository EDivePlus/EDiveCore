// Author: František Holubec
// Created: 09.10.2026

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.ProceduralUI
{
    // Cuts the SDFGraphic on the same object to a sector; fill and effects follow the cut
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SDFGraphic))]
    public class SDFArc : MonoBehaviour
    {
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private ArcCutout _Arc = ArcCutout.Default;

        private SDFGraphic _graphic;

        public SDFGraphic Graphic => _graphic ? _graphic : _graphic = GetComponent<SDFGraphic>();

        public ArcCutout Arc
        {
            get => _Arc;
            set { _Arc = value; SetDirty(); }
        }

        public float Value
        {
            get => _Arc.Value;
            set { if (Mathf.Approximately(_Arc.Value, value)) return; _Arc.Value = value; SetDirty(); }
        }

        private void OnEnable() => SetComponentsDirty();
        private void OnDisable() => SetComponentsDirty();
        private void OnDestroy() => SetComponentsDirty();
        private void OnValidate() => SetDirty();
        private void OnDidApplyAnimationProperties() => SetDirty();

        private void SetDirty()
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
}
