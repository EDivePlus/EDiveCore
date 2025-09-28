using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.EyeTracking
{
    public class EyeGazeController : MonoBehaviour
    {
        [SerializeField] 
        private EyeKind _Eye;
    
        [Range(0f, 1f)]
        [SerializeField] 
        private float _ConfidenceThreshold = 0.5f;
    
        [SerializeField] 
        private bool _ApplyPosition = true;

        [SerializeField] 
        private bool _ApplyRotation = true;
        
        [SerializeField] 
        private EyeSpaceKind _TrackingSpace;
        
        [ShowIf(nameof(_TrackingSpace), EyeSpaceKind.HeadSpace)]
        [SerializeField] 
        private Transform _ReferenceFrame;

        private EyeTrackingManager _eyeTrackingManager;
        private IDisposable _trackingSubscription;
        
        private void OnEnable() 
        {
            UniTask.Void(async() =>
            {
                _eyeTrackingManager = await AppCore.Services.AwaitRegistered<EyeTrackingManager>();
                var success = await _eyeTrackingManager.AwaitStartTracking(this);
                if (success)
                {
                    _trackingSubscription = _eyeTrackingManager.FrameEyeGazeStream.Subscribe(OnEyeGazeFrame);
                }
            });
        }

        private void OnDisable()
        {
            if (AppCore.Services.TryGet(out _eyeTrackingManager))
                _eyeTrackingManager?.StopTracking(this);
            
            _trackingSubscription?.Dispose();
        }

        private void OnEyeGazeFrame(EyeGazeFrame eyeGazeFrame)
        {
            var eyeGaze = eyeGazeFrame.GetEyeSample(_Eye, _TrackingSpace);
            if (!eyeGaze.IsValid)
                return;
            
            if (eyeGaze.Confidence < _ConfidenceThreshold)
                return;
            
            var pose = eyeGaze.GazePose;

            if (_ApplyPosition)
            {
                switch (_TrackingSpace)
                {
                    case EyeSpaceKind.HeadSpace: 
                        if (_ReferenceFrame != null)
                            transform.position = _ReferenceFrame.TransformPoint(pose.Position);
                        else
                            transform.localPosition = pose.Position;
                        break;
                    
                    case EyeSpaceKind.WorldSpace: 
                        transform.position = pose.Position;
                        break;
                    
                    default: 
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (_ApplyRotation)
            {
                switch (_TrackingSpace)
                {
                    case EyeSpaceKind.HeadSpace:
                        if (_ReferenceFrame != null)
                            transform.rotation = _ReferenceFrame.rotation * pose.Rotation;
                        else
                            transform.localRotation = pose.Rotation;
                        break;
                    
                    case EyeSpaceKind.WorldSpace:
                        transform.rotation = pose.Rotation;
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
