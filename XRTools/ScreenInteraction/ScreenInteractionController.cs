// Author: Michal Petr
// Created: 22.10.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Input.Controls;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools.ScreenInteraction
{
    // Targets interactables by tap, holds one at a time in the ScreenInteractor
    public class ScreenInteractionController : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private Camera _Camera;

        [SerializeField]
        [Required]
        private ScreenInteractor _Interactor;

        [SerializeField]
        [Required]
        private PointerTapDetector _TapDetector;

        [SerializeField]
        [MinValue(0f)]
        private float _MaxDistance = 3f;

        [SerializeField]
        private LayerMask _RaycastMask = ~0;

        public ScreenInteractable Target { get; private set; }
        public ScreenInteractable Held { get; private set; }
        public Camera Camera => _Camera;

        public event Action Changed;

        private void OnEnable()
        {
            _TapDetector.Tapped += OnTapped;
            _Interactor.selectEntered.AddListener(OnSelectEntered);
            _Interactor.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            Drop();
            SetTarget(null);
            _TapDetector.Tapped -= OnTapped;
            _Interactor.selectEntered.RemoveListener(OnSelectEntered);
            _Interactor.selectExited.RemoveListener(OnSelectExited);
        }

        // Grabbing while holding swaps, the held one is dropped
        public void Grab(XRGrabInteractable interactable)
        {
            if (interactable == null)
                return;

            Drop();
            SetTarget(null);
            _Interactor.StartManualInteraction((IXRSelectInteractable) interactable);
        }

        public void Drop()
        {
            if (_Interactor != null && _Interactor.isPerformingManualInteraction && _Interactor.hasSelection)
                _Interactor.EndManualInteraction();
        }

        public void Drop(XRBaseInteractable interactable)
        {
            if (interactable != null && _Interactor.IsSelecting((IXRSelectInteractable) interactable))
                Drop();
        }

        // Only what the screen controls hold can be activated
        public UniTask Activate(XRBaseInteractable interactable, CancellationToken cancellationToken)
        {
            if (interactable == null || !_Interactor.IsSelecting((IXRSelectInteractable) interactable))
                return UniTask.CompletedTask;
            return _Interactor.Activate(cancellationToken);
        }

        private void OnTapped(Vector2 screenPosition)
        {
            var ray = _Camera.ScreenPointToRay(screenPosition);
            var target = Physics.Raycast(ray, out var hit, _MaxDistance, _RaycastMask, QueryTriggerInteraction.Ignore)
                ? hit.collider.GetComponentInParent<ScreenInteractable>()
                : null;
            SetTarget(target == Held ? null : target);
        }

        private void OnSelectEntered(SelectEnterEventArgs args) => SetHeld(args.interactableObject.transform.GetComponentInParent<ScreenInteractable>());
        private void OnSelectExited(SelectExitEventArgs args) => SetHeld(null);

        private void SetTarget(ScreenInteractable target)
        {
            if (Target == target)
                return;

            Target = target;
            Changed?.Invoke();
        }

        private void SetHeld(ScreenInteractable held)
        {
            if (Held == held)
                return;

            Held = held;
            Changed?.Invoke();
        }
    }
}
