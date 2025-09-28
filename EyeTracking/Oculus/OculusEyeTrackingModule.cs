// Author: František Holubec
// Created: 18.09.2025

#if OCULUS_VR
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.External.Promises;
using R3;
using UnityEngine;

namespace EDIVE.EyeTracking.Oculus
{
    public class OculusEyeTrackingModule : AEyeTrackingModule
    {
        public override bool IsAvailable => OVRPlugin.eyeTrackingSupported;
        public override bool IsTracking => _trackingCancellation != null;
        public override Observable<EyeGazeFrame> EyeGazeStream => _eyeGazeStream;
        
        private readonly Subject<EyeGazeFrame> _eyeGazeStream = new();
  
        private readonly Promise _permissionPromise = new();
        private CancellationTokenSource _trackingCancellation;
        
        public override UniTask Initialize()
        {
            Debug.Log("[EyeTrackingManager] Oculus EyeTracking Module Initializing...");
            
            if (OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.EyeTracking))
            {
                _permissionPromise.Dispatch();
            }
            else
            {
                //OVRPermissionsRequester.Request(new[] {OVRPermissionsRequester.Permission.EyeTracking});
                OVRPermissionsRequester.PermissionGranted -= OnPermissionGranted;
                OVRPermissionsRequester.PermissionGranted += OnPermissionGranted;
            }

            Debug.Log("[EyeTrackingManager] Oculus EyeTracking Module Initialized");
            return UniTask.CompletedTask;
        }

        private void OnPermissionGranted(string permissionId)
        {
            if (permissionId != OVRPermissionsRequester.GetPermissionId(OVRPermissionsRequester.Permission.EyeTracking))
                return;
            
            _permissionPromise.Dispatch();
            OVRPermissionsRequester.PermissionGranted -= OnPermissionGranted;
        }
        
        public override void Terminate()
        {
            StopTracking();
        }

        public override void StartTracking(Action<bool> callback = null)
        {
            if (!IsAvailable)
            {
                Debug.Log($"[EyeTrackingManager] Oculus EyeTracking Unavailable.");
                callback?.Invoke(false);
                return;
            }
            
            if (IsTracking)
            {
                callback?.Invoke(true);
                return;
            }

            Debug.Log($"[EyeTrackingManager] Waiting for Oculus EyeTracking permission...");
            _permissionPromise.Then(() =>
            {
                if (!OVRPlugin.StartEyeTracking())
                {
                    Debug.LogWarning("[OculusEyeTrackingModule] Failed to start eye tracking.");
                    callback?.Invoke(false);
                    return;
                }

                _trackingCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                TrackingRoutine(_trackingCancellation.Token).Forget(); 
                callback?.Invoke(true);
            }); 
        }
        
        public override void StopTracking()
        {
            if (!IsTracking)
                return;

            OVRPlugin.StopEyeTracking();
            
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

        private static bool TrySampleGaze(out EyeGazeFrame eyeGazeFrame)
        {
            eyeGazeFrame = new EyeGazeFrame();
            
            OVRPlugin.EyeGazesState currentEyeGazesState = default;
            if (!OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref currentEyeGazesState))
                return false;
            
            var leftEyeGaze = SampleEyeFromGaze(currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Left]);
            var rightEyeGaze = SampleEyeFromGaze(currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Right]);

            var mainCam = Camera.main;
            var cameraPose = mainCam != null ? new Pose(mainCam.transform.position, mainCam.transform.rotation) : Pose.IDENTITY;

            eyeGazeFrame = new EyeGazeFrame(cameraPose, leftEyeGaze, rightEyeGaze, currentEyeGazesState.Time);
            return true;
        }
        
        private static EyeSample SampleEyeFromGaze(OVRPlugin.EyeGazeState eyeGaze)
        {
            if (!eyeGaze.IsValid)
                return EyeSample.INVALID;
            
            var gazePose = eyeGaze.Pose.ToOVRPose().ToHeadSpacePose();
            return new EyeSample(new Pose(gazePose.position, gazePose.orientation), eyeGaze.Confidence);
        }
    }
}
#endif
