// Author: František Holubec
// Created: 20.08.2025

using EDIVE.Conditions;
using UnityEngine;

namespace EDIVE.Input.Controls
{
    public abstract class AControls : MonoBehaviour
    {
        [SerializeReference]
        private ICondition _AvailabilityCondition;

        public bool CheckAvailable() => _AvailabilityCondition == null || _AvailabilityCondition.Evaluate();
        
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        
        public abstract void RequestTeleport(Vector3 position, Quaternion? rotation = null);
        
        protected virtual void Awake() { }
    }
}
