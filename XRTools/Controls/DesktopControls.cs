// Author: František Holubec
// Created: 20.08.2025

using EDIVE.XRTools.Controls.Legacy;
using UnityEngine;

namespace EDIVE.XRTools.Controls
{
    public class DesktopControls : AControls
    {
        [SerializeField]
        private DesktopCameraController _CameraController;

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
