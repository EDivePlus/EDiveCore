// Author: František Holubec
// Created: 07.06.2025

using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.Networking.XR
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class NetworkXRInteractable : NetworkBehaviour
    {
        private bool _wasKinematic;
        private Rigidbody _rigidbody;
        private XRBaseInteractable _interactable;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _interactable = GetComponent<XRBaseInteractable>();
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
            
            if (_rigidbody)
            {
                _wasKinematic = _rigidbody.isKinematic;
            }
        }

        private void OnDestroy()
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs arg0)
        {
            ResetInteractableVelocity();
            CmdPickup();
        }

        private void OnSelectExited(SelectExitEventArgs arg0)
        {
            if (_rigidbody)
            {
                _rigidbody.isKinematic = _wasKinematic;
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void CmdPickup(NetworkConnection sender = null)
        {
            ResetInteractableVelocity();
            if (sender?.ClientId != Owner.ClientId)
            {
                GiveOwnership(sender);
            }
        }
        
        private void ResetInteractableVelocity()
        {
            if (_rigidbody)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
}
