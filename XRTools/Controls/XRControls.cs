// Author: František Holubec
// Created: 20.08.2025

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace EDIVE.XRTools.Controls
{
    public class XRControls : AControls
    {
        [SerializeField]
        private TeleportationProvider _TeleportationProvider;
        
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
