using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDIVE.UIElements.Layout
{
    public enum RadialRotationMode
    {
        Inherit,
        Force,
        Ignore
    }

    [DisallowMultipleComponent]
    public class RadialLayoutElement : UIBehaviour, ILayoutIgnorer
    {
        [SerializeField]
        private bool _IgnoreLayout;

        [Tooltip("Inherit: use the layout group's 'Rotate Elements' setting. Force: always rotate outward. Ignore: never rotate.")]
        [SerializeField]
        private RadialRotationMode _RotationMode = RadialRotationMode.Inherit;

        [SerializeField]
        [MinValue(0f)]
        private float _Weight = 1f;

        [SerializeField]
        [MinMaxSlider(0f, 360f, true)]
        private Vector2 _AngularWidthRange = new(0f, 360f);

        [SerializeField]
        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = true)]
        private List<IRadialElementVisual> _Visuals = new();

        public RadialSliceInfo CurrentSlice { get; private set; }
        
        public bool IgnoreLayout
        {
            get => _IgnoreLayout;
            set
            {
                _IgnoreLayout = value; 
                SetDirty();
            }
        }

        public float Weight
        {
            get => _Weight;
            set
            {
                _Weight = value; 
                SetDirty();
            }
        }

        public float MinAngularWidth
        {
            get => _AngularWidthRange.x;
            set
            {
                _AngularWidthRange.x = Mathf.Clamp(value, 0f, _AngularWidthRange.y);
                SetDirty();
            }
        }

        public float MaxAngularWidth
        {
            get => _AngularWidthRange.y;
            set
            {
                _AngularWidthRange.y = Mathf.Clamp(value, _AngularWidthRange.x, 360f); 
                SetDirty();
            }
        }

        public RadialRotationMode RotationMode
        {
            get => _RotationMode;
            set
            {
                _RotationMode = value; 
                SetDirty();
            }
        }

        bool ILayoutIgnorer.ignoreLayout => _IgnoreLayout;

        public void ApplyLayout(in RadialSliceInfo info)
        {
            CurrentSlice = info;
            if (_Visuals == null) return;
            foreach (var visual in _Visuals)
                visual?.Apply(this, info);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (_AngularWidthRange.y < _AngularWidthRange.x) _AngularWidthRange.y = _AngularWidthRange.x;
            SetDirty();
        }
#endif

        protected override void OnEnable() { SetDirty(); }

        protected override void OnDisable() { SetDirty(); }

        protected override void OnTransformParentChanged() { SetDirty(); }

        protected override void OnDidApplyAnimationProperties() { SetDirty(); }

        protected override void OnBeforeTransformParentChanged() { SetDirty(); }

        private void SetDirty()
        {
            if (!IsActive())
                return;
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }
    }
}
