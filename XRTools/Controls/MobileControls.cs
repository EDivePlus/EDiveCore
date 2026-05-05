// Author: František Holubec
// Created: 05.05.2026

using EDIVE.XRTools.Controls.Mobile;
using UnityEngine;

namespace EDIVE.XRTools.Controls
{
    public class MobileControls : AControls
    {
        [SerializeField]
        private MobileCameraController _CameraController;

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
