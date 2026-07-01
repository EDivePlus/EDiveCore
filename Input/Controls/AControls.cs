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

        public abstract Vector3 Position { get; }
        public abstract Quaternion Rotation { get; }

        public bool CheckAvailable() => _AvailabilityCondition == null || _AvailabilityCondition.Evaluate();
        
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        
        public abstract void RequestTeleport(Vector3 position, Quaternion? rotation = null);

        public virtual void SetHeightMode(RigHeightMode mode) { }

        protected static Quaternion GetFloorRotation(Transform source)
        {
            var forward = source.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward, Vector3.up)
                : Quaternion.Euler(0f, source.eulerAngles.y, 0f);
        }
    }
}
