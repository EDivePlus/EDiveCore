// Author: František Holubec
// Created: 05.05.2026

using UnityEngine;

namespace EDIVE.Input.Controls
{
    public class UniversalControls : AControls
    {
        [SerializeField]
        private PlayerCameraController _CameraController;

        private float _defaultHeight;

        protected override void Awake()
        {
            _defaultHeight = _CameraController.transform.localPosition.y;
        }

        public override void RequestTeleport(Vector3 position, Quaternion? rotation = null)
        {
            _CameraController.Teleport(position + Vector3.up * _defaultHeight, rotation);
        }
    }
}
