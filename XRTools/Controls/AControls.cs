// Author: František Holubec
// Created: 20.08.2025

using UnityEngine;

namespace EDIVE.XRTools.Controls
{
    public abstract class AControls : MonoBehaviour
    {
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        
        public abstract void RequestTeleport(Vector3 position, Quaternion rotation);
        
        protected virtual void Awake() { }
    }
}
