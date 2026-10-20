// Author: Michal Petr
// Created: 20.10.2026

using EDIVE.ScriptableArchitecture.Variables.Impl;
using UnityEngine;

namespace EDIVE.XRTools.LaserPointer
{
    public class LaserPointerVisual : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer _Beam;

        [SerializeField]
        private Transform _Dot;

        [SerializeField]
        [Tooltip("Distance from the hand over which the beam fades to the tail alpha.")]
        private float _FadeLength = 0.2f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _TailAlpha = 0.12f;

        [SerializeField]
        [Tooltip("Dot size per meter of distance from the viewer.")]
        private float _DotSizePerMeter = 0.005f;

        [SerializeField]
        private float _MinDotSize = 0.004f;

        [SerializeField]
        [Tooltip("Smoothing of the end point when it is not set every frame.")]
        private float _EndPointSmoothing = 20f;

        [SerializeField]
        [Tooltip("Local viewer head. Falls back to the main camera.")]
        private TransformScriptableVariable _Viewer;

        private readonly GradientAlphaKey[] _alphaKeys = new GradientAlphaKey[3];
        private GradientColorKey[] _colorKeys;
        private Gradient _gradient;

        private Transform _anchor;
        private bool _smooth;
        private bool _snapEndPoint;
        private Vector3 _localOrigin;
        private Vector3 _targetEndPoint;
        private Vector3 _endPoint;
        private bool _hasHit;

        private void Awake()
        {
            _Beam.useWorldSpace = true;
            _Beam.positionCount = 3;
            _gradient = _Beam.colorGradient;
            _colorKeys = _gradient.colorKeys;
            Hide();
        }

        /// <summary>
        /// Shows the beam from a point relative to the anchor. Smooth for end points that arrive less often than every frame.
        /// </summary>
        public void Show(Transform anchor, bool smooth)
        {
            if (enabled && _anchor == anchor)
                return;

            _anchor = anchor;
            _smooth = smooth;
            _snapEndPoint = true;
            enabled = true;
        }

        public void SetBeam(Vector3 localOrigin, Vector3 endPoint, bool hasHit)
        {
            _snapEndPoint |= hasHit != _hasHit;
            _localOrigin = localOrigin;
            _targetEndPoint = endPoint;
            _hasHit = hasHit;
        }

        public void Hide()
        {
            enabled = false;
            _anchor = null;
            _Beam.enabled = false;
            _Dot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_anchor == null)
            {
                Hide();
                return;
            }

            _endPoint = _snapEndPoint || !_smooth
                ? _targetEndPoint
                : Vector3.Lerp(_endPoint, _targetEndPoint, 1f - Mathf.Exp(-_EndPointSmoothing * Time.deltaTime));
            _snapEndPoint = false;

            var start = _anchor.TransformPoint(_localOrigin);
            var toEnd = _endPoint - start;
            var length = toEnd.magnitude;
            var direction = length > 0f ? toEnd / length : _anchor.forward;
            var fadeLength = Mathf.Min(_FadeLength, length);

            _Beam.enabled = true;
            _Beam.SetPosition(0, start);
            _Beam.SetPosition(1, start + direction * fadeLength);
            _Beam.SetPosition(2, _endPoint);
            ApplyFade(length > 0f ? fadeLength / length : 1f);

            _Dot.gameObject.SetActive(_hasHit);
            if (_hasHit)
                PlaceDot(_endPoint, direction);
        }

        private void ApplyFade(float fadeEnd)
        {
            fadeEnd = Mathf.Clamp(fadeEnd, 0.001f, 0.999f);
            _alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            _alphaKeys[1] = new GradientAlphaKey(_TailAlpha, fadeEnd);
            _alphaKeys[2] = new GradientAlphaKey(_TailAlpha, 1f);
            _gradient.SetKeys(_colorKeys, _alphaKeys);
            _Beam.colorGradient = _gradient;
        }

        private void PlaceDot(Vector3 hitPoint, Vector3 beamDirection)
        {
            var viewer = GetViewer();
            var toViewer = viewer != null ? viewer.position - hitPoint : -beamDirection;
            var toViewerDirection = toViewer.normalized;
            var size = Mathf.Max(_MinDotSize, toViewer.magnitude * _DotSizePerMeter);

            _Dot.localScale = Vector3.one * size;
            _Dot.SetPositionAndRotation(hitPoint + toViewerDirection * (size * 0.5f), Quaternion.LookRotation(-toViewerDirection));
        }

        private Transform GetViewer()
        {
            if (_Viewer != null && _Viewer.Value != null)
                return _Viewer.Value;
            var camera = Camera.main;
            return camera != null ? camera.transform : null;
        }
    }
}
