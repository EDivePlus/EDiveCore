// Author: Michal Petr
// Created: 20.10.2026

using System;
using EDIVE.Core;
using EDIVE.XRTools.LaserPointer;
using PurrNet;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.Networking.XR
{
    public class NetworkLaserPointer : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("Synced hand that follows the local rig's left laser anchor.")]
        private Transform _LeftHand;

        [SerializeField]
        [Tooltip("Synced hand that follows the local rig's right laser anchor.")]
        private Transform _RightHand;

        [SerializeField]
        private LaserPointerVisual _Visual;

        private const float SEND_INTERVAL = 0.05f;

        private readonly SyncVar<LaserPointerBeamState> _beamState = new(sendIntervalInSeconds: SEND_INTERVAL, ownerAuth: true);

        private LaserPointerController _controller;
        private IDisposable _controllerRegistration;

        protected override void OnSpawned(bool asServer)
        {
            if (asServer)
                return;

            if (isOwner)
            {
                _controllerRegistration = AppCore.Services.WhenRegistered<LaserPointerController>(OnControllerRegistered);
            }
            else
            {
                _beamState.onChanged += OnBeamStateChanged;
                OnBeamStateChanged(_beamState.value);
            }
        }

        protected override void OnDespawned()
        {
            _controllerRegistration?.Dispose();
            _controllerRegistration = null;

            if (_controller != null)
            {
                _controller.FiringChanged -= OnLocalFiringChanged;
                _controller.BeamUpdated -= OnLocalBeamUpdated;
                _controller = null;
            }

            _beamState.onChanged -= OnBeamStateChanged;
            _Visual.Hide();
        }

        private void OnControllerRegistered(LaserPointerController controller)
        {
            _controller = controller;
            _controller.FiringChanged += OnLocalFiringChanged;
            _controller.BeamUpdated += OnLocalBeamUpdated;
            OnLocalFiringChanged(_controller.IsFiring);
        }

        private void OnLocalFiringChanged(bool firing)
        {
            if (!firing)
            {
                _beamState.value = default;
                _Visual.Hide();
                return;
            }

            _Visual.Show(_controller.Anchor, false);
            OnLocalBeamUpdated();
        }

        private void OnLocalBeamUpdated()
        {
            _beamState.value = new LaserPointerBeamState(_controller.ActiveHand, _controller.LocalOrigin, _controller.EndPoint, _controller.HasHit);
            _Visual.SetBeam(_controller.LocalOrigin, _controller.EndPoint, _controller.HasHit);
        }

        private void OnBeamStateChanged(LaserPointerBeamState state)
        {
            var hand = GetHand(state.Hand);
            if (hand == null)
            {
                _Visual.Hide();
                return;
            }

            _Visual.Show(hand, true);
            _Visual.SetBeam(state.LocalOrigin, state.EndPoint, state.HasHit);
        }

        private Transform GetHand(InteractorHandedness hand) => hand switch
        {
            InteractorHandedness.Left => _LeftHand,
            InteractorHandedness.Right => _RightHand,
            _ => null
        };
    }
}
