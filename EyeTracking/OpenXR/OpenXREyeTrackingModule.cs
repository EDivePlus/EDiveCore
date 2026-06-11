// Author: František Holubec
// Created: 07.05.2026

using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

#if UNITY_OPEN_XR
using System.Threading;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
#endif

namespace EDIVE.EyeTracking.OpenXR
{
    public class OpenXREyeTrackingModule : AEyeTrackingModule
    {
#if UNITY_OPEN_XR
        private const string EYE_TRACKING_PERMISSION = "com.oculus.permission.EYE_TRACKING";

        public override bool IsAvailable => IsEyeGazeFeatureEnabled();
        public override bool IsTracking => _trackingCancellation != null;
        public override Observable<EyeGazeFrame> EyeGazeStream => _eyeGazeStream;

        private readonly Subject<EyeGazeFrame> _eyeGazeStream = new();

        private CancellationTokenSource _trackingCancellation;
        private float _diagNextLogTime;

        public override UniTask Initialize()
        {
            Debug.Log("[EyeTrackingManager] OpenXR EyeTracking Module Initialized");
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

            RequestEyeTrackingPermission(granted =>
            {
                if (this == null)
                    return;

                if (!granted)
                {
                    Debug.LogWarning("[EyeTrackingManager] OpenXR EyeTracking permission denied.");
                    callback?.Invoke(false);
                    return;
                }

                if (!IsTracking)
                {
                    _trackingCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                    TrackingRoutine(_trackingCancellation.Token).Forget();
                    Debug.Log("[EyeTrackingManager] OpenXR EyeTracking started.");
                }
                callback?.Invoke(true);
            });
        }

        public override void StopTracking()
        {
            if (!IsTracking)
                return;

            _trackingCancellation?.Cancel();
            _trackingCancellation?.Dispose();
            _trackingCancellation = null;
        }

        private static void RequestEyeTrackingPermission(Action<bool> callback)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(EYE_TRACKING_PERMISSION))
            {
                callback(true);
                return;
            }

            Debug.Log("[EyeTrackingManager] Requesting OpenXR EyeTracking permission...");
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => callback(true);
            callbacks.PermissionDenied += _ => callback(false);
            Permission.RequestUserPermission(EYE_TRACKING_PERMISSION, callbacks);
#else
            callback(true);
#endif
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
            SampleEyesFromDevice(out var leftEye, out var rightEye);

            var mainCam = Camera.main;
            var cameraPose = mainCam != null
                ? new Pose(mainCam.transform.position, mainCam.transform.rotation)
                : Pose.IDENTITY;

            eyeGazeFrame = new EyeGazeFrame(cameraPose, leftEye, rightEye, Time.timeAsDouble);
            return true;
        }

        private void SampleEyesFromDevice(out EyeSample leftEye, out EyeSample rightEye)
        {
            leftEye = rightEye = EyeSample.INVALID;

            var eyeGazeDevice = InputSystem.GetDevice<EyeGazeInteraction.EyeGazeDevice>();
            var hmd = InputSystem.GetDevice<XRHMD>();
            var isTracked = eyeGazeDevice != null && eyeGazeDevice.pose.isTracked.isPressed;

            var gazePos = eyeGazeDevice != null ? eyeGazeDevice.pose.position.ReadValue() : Vector3.zero;
            var gazeRot = eyeGazeDevice != null ? eyeGazeDevice.pose.rotation.ReadValue() : Quaternion.identity;
            var headPos = hmd != null ? hmd.centerEyePosition.ReadValue() : Vector3.zero;
            var headRot = hmd != null ? hmd.centerEyeRotation.ReadValue() : Quaternion.identity;
            var leftEyePos = hmd != null ? hmd.leftEyePosition.ReadValue() : Vector3.zero;
            var rightEyePos = hmd != null ? hmd.rightEyePosition.ReadValue() : Vector3.zero;
            
            if (!isTracked || hmd == null)
                return;
            
            var invHeadRot = Quaternion.Inverse(headRot);
            var headSpaceGazeRot = invHeadRot * gazeRot;

            if (leftEyePos != Vector3.zero && rightEyePos != Vector3.zero)
            {
                leftEye = new EyeSample(new Pose(invHeadRot * (leftEyePos - headPos), headSpaceGazeRot), 1f);
                rightEye = new EyeSample(new Pose(invHeadRot * (rightEyePos - headPos), headSpaceGazeRot), 1f);
            }
            else
            {
                var combined = new EyeSample(new Pose(invHeadRot * (gazePos - headPos), headSpaceGazeRot), 1f);
                leftEye = rightEye = combined;
            }
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
