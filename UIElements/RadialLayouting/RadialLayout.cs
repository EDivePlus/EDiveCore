using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.UIElements.Layout
{
    public enum RadialDistribution
    {
        Start,
        Center,
        End,
        SpaceBetween,
    }

    public class RadialLayout : LayoutGroup
    {
        [SerializeField]
        [MinValue(0f)]
        private float _Radius = 100f;

        [Tooltip("Angle (degrees, measured from the top, growing counter-clockwise) where the arc begins.")]
        [Range(-360f, 360f)]
        [SerializeField]
        private float _StartAngle;

        [Tooltip("Total angular span the children are distributed across. 360 = full circle.")]
        [Range(0f, 360f)]
        [SerializeField]
        private float _SweepAngle = 360f;

        [Tooltip("How leftover sweep space (after width clamping) is distributed across the arc.")]
        [SerializeField]
        private RadialDistribution _Distribution = RadialDistribution.Start;

        [SerializeField]
        private bool _Clockwise;

        [Tooltip("Rotate each child so it faces outward from the center.")]
        [SerializeField]
        private bool _RotateElements;

        [Tooltip("Base rotation applied to every child (added on top of the outward facing rotation).")]
        [Range(-180f, 180f)]
        [SerializeField]
        private float _ElementRotation;

        private readonly struct Slice
        {
            public readonly float CenterAngle;
            public readonly float Width;
            public readonly float Rotation;

            public Slice(float centerAngle, float width, float rotation)
            {
                CenterAngle = centerAngle;
                Width = width;
                Rotation = rotation;
            }
        }

        private readonly List<RectTransform> _children = new();
        private readonly List<RadialLayoutElement> _elements = new();
        private readonly List<float> _widths = new();
        private readonly List<bool> _locked = new();
        private readonly List<Slice> _slices = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            CalculateRadial();
        }

        public override void SetLayoutHorizontal() { }

        public override void SetLayoutVertical() { }

        public override void CalculateLayoutInputVertical() { CalculateRadial(); }

        public override void CalculateLayoutInputHorizontal() { CalculateRadial(); }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            CalculateRadial();
        }
#endif

        private void CalculateRadial()
        {
            _children.Clear();
            _elements.Clear();
            _widths.Clear();
            _locked.Clear();
            _slices.Clear();

            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i) is not RectTransform child) continue;
                if (!child.gameObject.activeInHierarchy) continue;
                child.TryGetComponent<RadialLayoutElement>(out var element);
                if (element != null && element.IgnoreLayout) continue;

                _children.Add(child);
                _elements.Add(element);
                _widths.Add(0f);
                _locked.Add(false);
            }

            var n = _children.Count;
            if (n == 0) return;

            var gapCount = Mathf.Max(0, n - 1);
            var availableSweep = Mathf.Max(0f, _SweepAngle);

            AllocateWidths(availableSweep);

            var contentSum = 0f;
            for (var i = 0; i < n; i++) contentSum += _widths[i];
            var leftover = Mathf.Max(0f, availableSweep - contentSum);

            var startOffset = 0f;
            var gap = 0f;
            switch (_Distribution)
            {
                case RadialDistribution.Start:
                    break;
                case RadialDistribution.End:
                    startOffset = leftover;
                    break;
                case RadialDistribution.Center:
                    startOffset = leftover * 0.5f;
                    break;
                case RadialDistribution.SpaceBetween:
                    if (gapCount > 0) gap += leftover / gapCount;
                    break;
            }

            var direction = _Clockwise ? -1f : 1f;
            const float topOffset = 90f;
            var cursor = _StartAngle + direction * startOffset;

            for (var i = 0; i < n; i++)
            {
                var width = _widths[i];
                var center = cursor + direction * width * 0.5f;
                cursor += direction * (width + gap);

                var child = _children[i];
                var element = _elements[i];

                var placeAngle = topOffset + center;
                var rad = placeAngle * Mathf.Deg2Rad;
                child.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _Radius;

                var rotation = _ElementRotation;
                var mode = element != null ? element.RotationMode : RadialRotationMode.Inherit;
                var doRotate = mode switch
                {
                    RadialRotationMode.Force => true,
                    RadialRotationMode.Ignore => false,
                    _ => _RotateElements
                };
                if (doRotate) rotation += placeAngle - 90f;
                child.localEulerAngles = new Vector3(0f, 0f, rotation);

                _slices.Add(new Slice(center, width, rotation));

                if (element != null) element.ApplyLayout(new RadialSliceInfo(center, width, _Radius));
            }
        }

        private void AllocateWidths(float availableSweep)
        {
            var n = _children.Count;

            var safety = n + 1;
            var changed = true;
            while (changed && safety-- > 0)
            {
                changed = false;
                var lockedSum = 0f;
                var unlockedWeight = 0f;
                var unlockedCount = 0;
                for (var i = 0; i < n; i++)
                {
                    if (_locked[i]) lockedSum += _widths[i];
                    else
                    {
                        unlockedWeight += GetWeight(_elements[i]);
                        unlockedCount++;
                    }
                }

                if (unlockedCount == 0) break;

                var available = Mathf.Max(0f, availableSweep - lockedSum);

                for (var i = 0; i < n; i++)
                {
                    if (_locked[i]) continue;
                    var element = _elements[i];
                    var weight = GetWeight(element);
                    var min = GetMin(element);
                    var max = GetMax(element);
                    var proposed = unlockedWeight > Mathf.Epsilon
                        ? weight / unlockedWeight * available
                        : 0f;

                    if (proposed < min)
                    {
                        _widths[i] = min;
                        _locked[i] = true;
                        changed = true;
                    }
                    else if (proposed > max)
                    {
                        _widths[i] = max;
                        _locked[i] = true;
                        changed = true;
                    }
                    else
                    {
                        _widths[i] = proposed;
                    }
                }
            }
        }

        private static float GetWeight(RadialLayoutElement e) => e != null ? Mathf.Max(0f, e.Weight) : 1f;
        private static float GetMin(RadialLayoutElement e) => e != null ? Mathf.Max(0f, e.MinAngularWidth) : 0f;
        private static float GetMax(RadialLayoutElement e) => e != null ? Mathf.Max(GetMin(e), e.MaxAngularWidth) : 360f;

#if UNITY_EDITOR
        [PropertyOrder(100)]
        [PropertySpace]
        [SerializeField]
        private bool _DrawGizmos = true;

        private void OnDrawGizmosSelected()
        {
            if (!_DrawGizmos || _slices.Count == 0) return;

            var rectT = (RectTransform) transform;
            var outerRadius = Mathf.Min(rectT.rect.width, rectT.rect.height) * 0.5f;
            if (outerRadius <= 0f) outerRadius = _Radius;

            var prevColor = Handles.color;
            var prevMatrix = Handles.matrix;
            Handles.matrix = rectT.localToWorldMatrix;

            const float topOffset = 90f;

            Handles.color = Color.cyan;
            foreach (var slice in _slices)
            {
                var startAngle = topOffset + slice.CenterAngle - slice.Width * 0.5f;
                var fromDir = Quaternion.Euler(0f, 0f, startAngle) * Vector3.right;
                var toDir = Quaternion.Euler(0f, 0f, startAngle + slice.Width) * Vector3.right;

                Handles.DrawWireArc(Vector3.zero, Vector3.forward, fromDir, slice.Width, outerRadius);
                Handles.DrawLine(Vector3.zero, fromDir.normalized * outerRadius);
                Handles.DrawLine(Vector3.zero, toDir.normalized * outerRadius);
            }

            var markerSize = Mathf.Max(outerRadius, _Radius) * 0.06f;
            foreach (var slice in _slices)
            {
                var placeAngle = topOffset + slice.CenterAngle;
                var rad = placeAngle * Mathf.Deg2Rad;
                var center = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _Radius;

                var rot = Quaternion.Euler(0f, 0f, slice.Rotation);
                var right = rot * Vector3.right;
                var up = rot * Vector3.up;

                Handles.DrawSolidDisc(center, Vector3.forward, markerSize * 0.2f);
                Handles.DrawLine(center - right * markerSize, center + right * markerSize);
                Handles.DrawLine(center - up * markerSize, center + up * markerSize * 1.6f);
            }

            Handles.matrix = prevMatrix;
            Handles.color = prevColor;
        }
#endif
    }
}
