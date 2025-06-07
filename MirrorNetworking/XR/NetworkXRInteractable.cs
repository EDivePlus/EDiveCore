// Author: František Holubec
// Created: 07.06.2025

using Mirror;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.MirrorNetworking.XR
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class NetworkXRInteractable : NetworkBehaviour
    {
        private Rigidbody _rigidbody;
        private XRBaseInteractable _interactable;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _interactable = GetComponent<XRBaseInteractable>();
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
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
            //CmdDrop();
        }

        [Command(requiresAuthority = false)]
        public void CmdPickup(NetworkConnectionToClient sender = null)
        {
            ResetInteractableVelocity();
            if (sender != netIdentity.connectionToClient)
            {
                netIdentity.RemoveClientAuthority();
                netIdentity.AssignClientAuthority(sender);
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdDrop(NetworkConnectionToClient sender = null)
        {

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
