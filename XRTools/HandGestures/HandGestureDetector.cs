// Author: František Holubec
// Created: 13.03.2026

#if XR_HANDS
using System;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.ToggleStates;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;

namespace EDIVE.XRTools.HandGestures
{
    public class HandGestureDetector : MonoBehaviour
    {
        [SerializeField]
        private XRHandTrackingEvents _HandTrackingEvents;
        
        [SerializeReference]
        private IHandGesture _Gesture;
        
        [SerializeField]
        private float _MinimumHoldTime = 0.2f;

        [SerializeField]
        private float _DetectionInterval = 0.1f;

        [SerializeField]
        private AToggleState _GestureState;
        
        [ShowInInspector, ReadOnly]
        public bool IsPerforming { get; private set; }
        
        public event Action GesturePerformed;
        public event Action GestureEnded;

        [ShowInInspector, ReadOnly, KeepRefreshing]
        private bool _isDetected;
        private IDisposable _jointsUpdatedHandle;
        private IDisposable _holdHandle;
        
        private void OnEnable()
        {
            if(_GestureState)
                _GestureState.SetState(false);
            
            if (_HandTrackingEvents == null || _Gesture == null || !_Gesture.CheckValid())
                return;
            
            _jointsUpdatedHandle = _HandTrackingEvents.jointsUpdated.AsObservable()
                .ThrottleLast(TimeSpan.FromSeconds(_DetectionInterval))
                .Select(args => _HandTrackingEvents.handIsTracked && _Gesture.CheckConditions(args))
                .DistinctUntilChanged()
                .Subscribe(SetDetected);
        }
        
        private void OnDisable()
        {
            _jointsUpdatedHandle?.Dispose();
            _holdHandle?.Dispose();
            SetPerforming(false);
        }
        
        private void SetDetected(bool detected)
        {
            if (_isDetected == detected) return;
            _isDetected = detected;
            
            _holdHandle?.Dispose();
            _holdHandle = null;
            
            if (!detected)
            {
                SetPerforming(false);
            }
            else
            {
                _holdHandle = Observable.Timer(TimeSpan.FromSeconds(_MinimumHoldTime))
                    .Subscribe(_ => SetPerforming(true));
            }
        }
        
        private void SetPerforming(bool isPerforming)
        {
            if (IsPerforming == isPerforming)
                return;
            
            IsPerforming = isPerforming;
            if(_GestureState)
                _GestureState.SetState(isPerforming);
            
            if (isPerforming)
                GesturePerformed?.Invoke();
            else
                GestureEnded?.Invoke();
        }
    }

    public interface IHandGesture
    {
        bool CheckValid();
        bool CheckConditions(XRHandJointsUpdatedEventArgs eventArgs);
    }
    
    [Serializable]
    public class HandShapeGesture : IHandGesture
    {
        [SerializeField]
        private XRHandShape _HandShape;

        public bool CheckValid() => _HandShape != null;
        public bool CheckConditions(XRHandJointsUpdatedEventArgs eventArgs)
        {
            return _HandShape.CheckConditions(eventArgs);
        }
    }
    
    [Serializable]
    public class HandPoseGesture : IHandGesture
    {
        [SerializeField]
        private XRHandPose _HandPose;
        
        [SerializeField]
        private Transform _Target;
        
        public bool CheckValid() => _HandPose != null;
        public bool CheckConditions(XRHandJointsUpdatedEventArgs eventArgs)
        {
            _HandPose.relativeOrientation.targetTransform = _Target;
            return _HandPose.CheckConditions(eventArgs);
        }
    }
}
#endif
