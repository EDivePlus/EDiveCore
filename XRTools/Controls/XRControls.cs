// Author: František Holubec
// Created: 20.08.2025

using EDIVE.Input.Controls;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace EDIVE.XRTools.Controls
{
    public class XRControls : AControls
    {
        [SerializeField]
        private TeleportationProvider _TeleportationProvider;

        [SerializeField]
        private XROrigin _XROrigin;

        public override Vector3 Position
        {
            get
            {
                if (_XROrigin == null || _XROrigin.Camera == null) return transform.position;
                var camPos = _XROrigin.Camera.transform.position;
                var floorY = _XROrigin.Origin.transform.position.y;
                return new Vector3(camPos.x, floorY, camPos.z);
            }
        }

        public override Quaternion Rotation => _XROrigin != null && _XROrigin.Camera != null ? GetFloorRotation(_XROrigin.Camera.transform) : transform.rotation;

        public override void RequestTeleport(Vector3 position, Quaternion? rotation = null)
        {
            _TeleportationProvider.QueueTeleportRequest(new TeleportRequest
            {
                destinationPosition = position,
                destinationRotation = rotation ?? Quaternion.identity,
                matchOrientation = rotation.HasValue ? MatchOrientation.TargetUpAndForward : MatchOrientation.None
            });
        }
    }
}
