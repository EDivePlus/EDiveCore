// Author: František Holubec
// Created: 02.09.2026

using System;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Utils.Activations
{
    [Serializable]
    public class ColliderActivation : AWrapperActivation
    {
        [SerializeField]
        private ColliderEvents _Collider;
        
        [SerializeField]
        private CollisionEvent _EventType;

        public enum CollisionEvent
        {
            TriggerEnter,
            TriggerExit,
            ColliderEnter,
            ColliderExit
        }
        
        protected override void StartListening()
        {
            if (_Collider == null) 
                return;
            switch (_EventType)
            {
                case CollisionEvent.TriggerEnter:
                    _Collider.TriggerEntered += OnTriggerEvent;
                    break;
                case CollisionEvent.TriggerExit: 
                    _Collider.TriggerExited += OnTriggerEvent;
                    break;
                case CollisionEvent.ColliderEnter: 
                    _Collider.ColliderEntered += OnCollisionEvent;
                    break;
                case CollisionEvent.ColliderExit: 
                    _Collider.ColliderExited += OnCollisionEvent;
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        protected override void StopListening()
        {
            if (_Collider == null) return;
            _Collider.TriggerEntered -= OnTriggerEvent;
            _Collider.TriggerExited -= OnTriggerEvent;
            _Collider.ColliderEntered -= OnCollisionEvent;
            _Collider.ColliderExited -= OnCollisionEvent;
        }
        
        private void OnCollisionEvent(Collision collision) => InvokeListeners();
        private void OnTriggerEvent(Collider collider) => InvokeListeners();
    }
}
