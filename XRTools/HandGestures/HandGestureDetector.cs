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

namespace EDIVE.XRTools.HandGestures
{
    public class HandGestureDetector : MonoBehaviour
    {
        [SerializeField]
        private XRHandTrackingEvents _HandTrackingEvents;
        
        [SerializeReference]
        private IHandGesture _TriggerGesture;

        [SerializeField]
        private bool _UseSeparateHoldGesture; 
            
        [EnableIf(nameof(_UseSeparateHoldGesture))]
        [SerializeReference]
        private IHandGesture _HoldGesture;
        
        [SerializeField]
        private float _MinimumHoldTime = 0.2f;

        [SerializeField]
        private float _DetectionInterval = 0.1f;

        [SerializeField]
        private AToggleState _GestureState;
        
        [ShowInInspector, ReadOnly, KeepRefreshing]
        public bool IsPerforming { get; private set; }
        
        public event Action PerformStarted;
        public event Action PerformEnded;

        [ShowInInspector, ReadOnly, KeepRefreshing]
        private bool _isDetected;
        
        private IDisposable _jointsUpdatedHandle;
        private IDisposable _holdHandle;
        
        private void OnEnable()
        {
            if(_GestureState)
                _GestureState.SetState(false);
            
            if (_HandTrackingEvents == null || _TriggerGesture == null || !_TriggerGesture.CheckValid())
                return;
            
            _jointsUpdatedHandle = _HandTrackingEvents.jointsUpdated.AsObservable()
                .ThrottleLast(TimeSpan.FromSeconds(_DetectionInterval))
                .Select(CheckGesture)
                .DistinctUntilChanged()
                .Subscribe(SetDetected);
        }

        private bool CheckGesture(XRHandJointsUpdatedEventArgs args)
        {
            if (!_HandTrackingEvents.handIsTracked)
                return false;

            if (_isDetected && _UseSeparateHoldGesture && _HoldGesture != null && _HoldGesture.CheckValid())
                return _HoldGesture.CheckConditions(args);

            return _TriggerGesture.CheckConditions(args);
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
                PerformStarted?.Invoke();
            else
                PerformEnded?.Invoke();
        }
    }
}
#endif
