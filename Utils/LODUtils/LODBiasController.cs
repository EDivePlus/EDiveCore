// Author: František Holubec
// Created: 07.06.2026

using System.Collections.Generic;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR;

namespace EDIVE.Utils.LODUtils
{
    [DisallowMultipleComponent]
    public class LODBiasController : MonoBehaviour
    {
        [SerializeField]
        private GameObjectScriptableVariable _CameraSource;
        
        [Tooltip("Reference FOV used in project - usually ~60°")]
        [SuffixLabel("°", true)]
        [PropertyRange(1f, 180f)]
        [SerializeField]
        private float _ReferenceFOV = 60f;
        
        [PropertySpace]
        [SerializeField]
        private bool _ClampBias = true;
        
        [ShowIf(nameof(_ClampBias))]
        [MinMaxSlider(0.05f, 20f, true)]
        [SerializeField]
        private Vector2 _BiasRange = new(0.1f, 4f);
        
        [PropertySpace]
        [SuffixLabel("°", true)]
        [ReadOnly]
        [ShowInInspector]
        private float DetectedFov => Application.isPlaying && _resolvedCamera != null ? ResolveFov() : 0f;

        [ReadOnly]
        [ShowInInspector]
        private float CurrentBias => QualitySettings.lodBias;

        private readonly Dictionary<int, float> _defaultBiasByLevel = new();
        private int _lastAppliedLevel = -1;

        private Camera _resolvedCamera;
        private float _lastFov = float.NaN;

        private void OnEnable()
        {
            _lastAppliedLevel = -1;
            _lastFov = float.NaN;

            if (_CameraSource != null)
                _CameraSource.ValueChanged += OnCameraSourceChanged;

            RefreshCamera();
            Apply();
        }

        private void OnDisable()
        {
            if (_CameraSource != null)
                _CameraSource.ValueChanged -= OnCameraSourceChanged;

            var level = QualitySettings.GetQualityLevel();
            if (_defaultBiasByLevel.TryGetValue(level, out var origVal))
                QualitySettings.lodBias = origVal;
        }

        private void Update()
        {
            if (QualitySettings.GetQualityLevel() != _lastAppliedLevel || HasFovChanged())
                Apply();
        }

        private void OnCameraSourceChanged()
        {
            RefreshCamera();
            Apply();
        }

        [Button]
        [DisableInEditorMode]
        public void Apply()
        {
            if (_resolvedCamera == null)
                RefreshCamera();
            if (_resolvedCamera == null)
                return;

            var level = QualitySettings.GetQualityLevel();

            if (!_defaultBiasByLevel.TryGetValue(level, out var authoredBias))
            {
                authoredBias = QualitySettings.lodBias;
                _defaultBiasByLevel[level] = authoredBias;
            }
            
            var fov = ResolveFov();
            var target = authoredBias * GetCorrectionFactor(fov);

            if (_ClampBias)
                target = Mathf.Clamp(target, _BiasRange.x, _BiasRange.y);

            if (!Mathf.Approximately(QualitySettings.lodBias, target))
                QualitySettings.lodBias = target;

            _lastAppliedLevel = level;
            _lastFov = fov;
        }

        private void RefreshCamera()
        {
            _resolvedCamera = ResolveCamera();
            _lastFov = float.NaN;
        }

        private Camera ResolveCamera()
        {
            if (_CameraSource != null && _CameraSource.Value != null && _CameraSource.Value.TryGetComponent(out Camera fromSource)) 
                return fromSource;
            return TryGetComponent(out Camera local) ? local : Camera.main;
        }

        private bool HasFovChanged()
        {
            if (_resolvedCamera == null)
                return false;

            var fov = ResolveFov();
            return float.IsNaN(_lastFov) || !Mathf.Approximately(fov, _lastFov);
        }

        private float ResolveFov()
        {
            var cam = _resolvedCamera;
            if (cam == null)
                return _ReferenceFOV;

            var proj = cam.projectionMatrix;
            if (XRSettings.enabled && cam.stereoEnabled)
            {
                var stereo = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                if (stereo.m11 > 0.0001f)
                    proj = stereo;
            }

            var m11 = proj.m11;
            if (m11 <= 0.0001f)
                return cam.fieldOfView;

            return Mathf.Atan(1f / m11) * 2f * Mathf.Rad2Deg;
        }

        private float GetCorrectionFactor(float actualFovDegrees)
        {
            var refRad = _ReferenceFOV * Mathf.Deg2Rad;
            var actRad = actualFovDegrees * Mathf.Deg2Rad;
            var refTan = Mathf.Tan(refRad * 0.5f);
            if (refTan <= Mathf.Epsilon)
                return 1f;
            return Mathf.Tan(actRad * 0.5f) / refTan;
        }
    }
}
