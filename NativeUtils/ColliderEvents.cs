// Author: František Holubec
// Created: 04.09.2025

using System;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public class ColliderEvents : MonoBehaviour
    {
        public event Action<Collider> TriggerEntered;
        public event Action<Collider> TriggerExited;
        public event Action<Collider> TriggerStayed;
        
        public event Action<Collision> ColliderEntered;
        public event Action<Collision> ColliderExited;
        public event Action<Collision> ColliderStayed;

        private void OnTriggerEnter(Collider other) => TriggerEntered?.Invoke(other);
        private void OnTriggerExit(Collider other) => TriggerExited?.Invoke(other);
        private void OnTriggerStay(Collider other) => TriggerStayed?.Invoke(other);
        private void OnCollisionEnter(Collision other) => ColliderEntered?.Invoke(other);
        private void OnCollisionExit(Collision other) => ColliderExited?.Invoke(other);
        private void OnCollisionStay(Collision other) => ColliderStayed?.Invoke(other);
    }
}
