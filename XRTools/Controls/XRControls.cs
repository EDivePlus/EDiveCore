// Author: František Holubec
// Created: 20.08.2025

using System;
using System.Collections.Generic;
using EDIVE.Input.Controls;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
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

        [SerializeField]
        [EnhancedTableList]
        private List<RigHeightSetting> _HeightModes = new()
        {
            new RigHeightSetting(RigHeightMode.Seated, XROrigin.TrackingOriginMode.Device, 1.1f),
            new RigHeightSetting(RigHeightMode.Standing, XROrigin.TrackingOriginMode.Device, 1.5f),
            new RigHeightSetting(RigHeightMode.Floor, XROrigin.TrackingOriginMode.Floor, 0f)
        };

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

        [PropertySpace]
        [ShowInInspector]
        [EnumToggleButtons]
        public RigHeightMode HeightMode
        {
            get => _heightMode;
            set => SetHeightMode(value);
        }

        private RigHeightMode _heightMode;

        private void Start()
        {
            SetHeightMode(ControlsManager.SavedHeightMode);
        }

        public override void RequestTeleport(Vector3 position, Quaternion? rotation = null)
        {
            _TeleportationProvider.QueueTeleportRequest(new TeleportRequest
            {
                destinationPosition = position,
                destinationRotation = rotation ?? Quaternion.identity,
                matchOrientation = rotation.HasValue ? MatchOrientation.TargetUpAndForward : MatchOrientation.None
            });
        }

        public override void SetHeightMode(RigHeightMode mode)
        {
            if (_XROrigin == null)
                return;

            if (!_HeightModes.TryGetFirst(s => s.Mode == mode, out var setting))
                return;
            
            _heightMode = mode;
            _XROrigin.RequestedTrackingOriginMode = setting.TrackingMode;
            _XROrigin.CameraYOffset = setting.EyeHeight;

            var offsetObject = _XROrigin.CameraFloorOffsetObject;
            if (offsetObject != null)
            {
                var local = offsetObject.transform.localPosition;
                local.y = setting.EyeHeight;
                offsetObject.transform.localPosition = local;
            }
        }

        [Serializable]
        private class RigHeightSetting
        {
            [SerializeField]
            private RigHeightMode _Mode;

            [SerializeField]
            private XROrigin.TrackingOriginMode _TrackingMode;

            [SerializeField]
            [SuffixLabel("m", true)]
            private float _EyeHeight;

            public RigHeightMode Mode => _Mode;
            public XROrigin.TrackingOriginMode TrackingMode => _TrackingMode;
            public float EyeHeight => _EyeHeight;

            public RigHeightSetting(RigHeightMode mode, XROrigin.TrackingOriginMode trackingMode, float eyeHeight)
            {
                _Mode = mode;
                _TrackingMode = trackingMode;
                _EyeHeight = eyeHeight;
            }
        }
    }
}
