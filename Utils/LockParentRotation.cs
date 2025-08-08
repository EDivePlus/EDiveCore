// Author: František Holubec
// Created: 08.08.2025

using UnityEngine;

namespace EDIVE.Utils
{
    public class LockParentRotation : MonoBehaviour
    {
        [SerializeField]
        private bool _LockX;
        [SerializeField]
        private bool _LockY;
        [SerializeField]
        private bool _LockZ;

        private void LateUpdate()
        {
            var rot = transform.parent.eulerAngles;
            if (_LockX) rot.x = 0;
            if (_LockY) rot.y = 0;
            if (_LockZ) rot.z = 0;
            transform.eulerAngles = rot;
        }
    }
}
