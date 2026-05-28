// Author: František Holubec
// Created: 07.05.2026

using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_OPEN_XR
using System.Threading;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
#endif

namespace EDIVE.EyeTracking.OpenXR
{
    public class OpenXREyeTrackingModule : AEyeTrackingModule
    {
        [SerializeField]
        private InputActionReference _GazePosition;

        [SerializeField]
        private InputActionReference _GazeRotation;

        [SerializeField]
        private InputActionReference _GazeIsTracked;

#if UNITY_OPEN_XR
        public override bool IsAvailable => IsEyeGazeFeatureEnabled() && _GazePosition != null && _GazeRotation != null && _GazeIsTracked != null;
        public override bool IsTracking => _trackingCancellation != null;
        public override Observable<EyeGazeFrame> EyeGazeStream => _eyeGazeStream;

        private readonly Subject<EyeGazeFrame> _eyeGazeStream = new();

        private CancellationTokenSource _trackingCancellation;

        public override UniTask Initialize()
        {
            Debug.Log("[EyeTrackingManager] OpenXR EyeTracking Module Initialized");
            _GazePosition.action.actionMap.Enable();
            _GazePosition.action.Enable();
            _GazeRotation.action.Enable();
            _GazeIsTracked.action.Enable();
            return UniTask.CompletedTask;
        }

        public override void Terminate()
        {
            StopTracking();
        }

        public override void StartTracking(Action<bool> callback = null)
        {
            if (!IsAvailable)
            {
                Debug.Log("[EyeTrackingManager] OpenXR EyeTracking Unavailable.");
                callback?.Invoke(false);
                return;
            }

            if (IsTracking)
            {
                callback?.Invoke(true);
                return;
            }
            
            _trackingCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            TrackingRoutine(_trackingCancellation.Token).Forget();
            callback?.Invoke(true);
        }

        public override void StopTracking()
        {
            if (!IsTracking)
                return;

            _trackingCancellation?.Cancel();
            _trackingCancellation?.Dispose();
            _trackingCancellation = null;
        }

        private async UniTaskVoid TrackingRoutine(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                if (TrySampleGaze(out var gazeFrame))
                {
                    _eyeGazeStream.OnNext(gazeFrame);
                }
            }
        }

        private bool TrySampleGaze(out EyeGazeFrame eyeGazeFrame)
        {
            eyeGazeFrame = new EyeGazeFrame();

            var sample = SampleEyeFromAction();

            var mainCam = Camera.main;
            var cameraPose = mainCam != null
                ? new Pose(mainCam.transform.position, mainCam.transform.rotation)
                : Pose.IDENTITY;

            // OpenXR EyeGazeInteraction provides only a single combined gaze — report it for both eyes.
            eyeGazeFrame = new EyeGazeFrame(cameraPose, sample, sample, Time.timeAsDouble);
            return true;
        }

        private EyeSample SampleEyeFromAction()
        {
            if (_GazeIsTracked.action.ReadValue<float>() < 0.5f)
                return EyeSample.INVALID;

            var hmd = InputSystem.GetDevice<XRHMD>();
            if (hmd == null)
                return EyeSample.INVALID;

            var gazePos = _GazePosition.action.ReadValue<Vector3>();
            var gazeRot = _GazeRotation.action.ReadValue<Quaternion>();

            // Both gaze and HMD poses are in OpenXR tracking space — transform gaze into head-local
            // space so the existing EyeSample.ToWorldSpace(cameraPose) path keeps working.
            var hmdPos = hmd.centerEyePosition.ReadValue();
            var hmdRot = hmd.centerEyeRotation.ReadValue();
            var invHmdRot = Quaternion.Inverse(hmdRot);
            var headRelPos = invHmdRot * (gazePos - hmdPos);
            var headRelRot = invHmdRot * gazeRot;

            return new EyeSample(new Pose(headRelPos, headRelRot), 1f);
        }

        private static bool IsEyeGazeFeatureEnabled()
        {
            var settings = OpenXRSettings.Instance;
            if (settings == null) return false;
            var feature = settings.GetFeature<EyeGazeInteraction>();
            return feature != null && feature.enabled;
        }
#else
        public override bool IsAvailable => false;
        public override bool IsTracking => false;

        public override Observable<EyeGazeFrame> EyeGazeStream => null;
        public override UniTask Initialize() => UniTask.CompletedTask;
        public override void Terminate() { }
        public override void StartTracking(Action<bool> callback = null) { }
        public override void StopTracking() { }
#endif
    }
}
