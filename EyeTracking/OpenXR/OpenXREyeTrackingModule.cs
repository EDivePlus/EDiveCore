// Author: František Holubec
// Created: 07.05.2026

using System;
using Cysharp.Threading.Tasks;
using R3;

#if UNITY_OPEN_XR
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
#endif

namespace EDIVE.EyeTracking.OpenXR
{
    public class OpenXREyeTrackingModule : AEyeTrackingModule
    {
#if UNITY_OPEN_XR
        public override bool IsAvailable => IsEyeGazeFeatureEnabled();
        public override bool IsTracking => _trackingCancellation != null;
        public override Observable<EyeGazeFrame> EyeGazeStream => _eyeGazeStream;

        private readonly Subject<EyeGazeFrame> _eyeGazeStream = new();

        private CancellationTokenSource _trackingCancellation;

        private InputAction _gazePoseAction;

        public override UniTask Initialize()
        {
            Debug.Log("[EyeTrackingManager] OpenXR EyeTracking Module Initializing...");

            _gazePoseAction = new InputAction("EyeGazePose", InputActionType.Value, "<EyeGaze>/pose", expectedControlType: "Pose");

            Debug.Log("[EyeTrackingManager] OpenXR EyeTracking Module Initialized");
            return UniTask.CompletedTask;
        }

        public override void Terminate()
        {
            StopTracking();
            _gazePoseAction?.Dispose();
            _gazePoseAction = null;
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

            _gazePoseAction.Enable();

            _trackingCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            TrackingRoutine(_trackingCancellation.Token).Forget();
            callback?.Invoke(true);
        }

        public override void StopTracking()
        {
            if (!IsTracking)
                return;

            _gazePoseAction?.Disable();

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

            if (_gazePoseAction == null)
                return false;

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
            var poseState = _gazePoseAction.ReadValue<PoseState>();
            if (!poseState.isTracked)
                return EyeSample.INVALID;

            var hmd = InputSystem.GetDevice<XRHMD>();  
            if (hmd == null)
                return EyeSample.INVALID;

            // Both gaze and HMD poses are in OpenXR tracking space — transform gaze into head-local
            // space so the existing EyeSample.ToWorldSpace(cameraPose) path keeps working.
            var hmdPos = hmd.centerEyePosition.ReadValue();
            var hmdRot = hmd.centerEyeRotation.ReadValue();
            var invHmdRot = Quaternion.Inverse(hmdRot);
            var headRelPos = invHmdRot * (poseState.position - hmdPos);
            var headRelRot = invHmdRot * poseState.rotation;

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