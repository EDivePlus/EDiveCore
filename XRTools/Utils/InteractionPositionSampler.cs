// Author: Radim Holub, František Holubec
// Created: 18.03.2026

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Vector3 = UnityEngine.Vector3;

namespace EDIVE.XRTools.Utils
{
    public class InteractionPositionSampler : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private XRBaseInteractable _Interactable;
        
        [SerializeField]
        private bool _ShowTargetPoints = true;

        [SerializeField]
        private Vector3 _SurfaceOffset = Vector3.zero;

        [Required]
        [SerializeField]
        private Collider _Collider;

        [SerializeField]
        private List<Transform> _AvailableTargetPoints;
        
        private readonly List<IXRHoverInteractor> _currentInteractors = new();
        
        private readonly List<Vector3> _currentPositions = new();
        public IEnumerable<Vector3> CurrentPositions => _currentPositions;
        
        public bool ShowTargetPoints
        {
            get => _ShowTargetPoints;
            set
            {
                _ShowTargetPoints = value;
                for (var i = 0; i < _AvailableTargetPoints.Count; i++)
                {
                    _AvailableTargetPoints[i].gameObject.SetActive(_ShowTargetPoints && _currentPositions.Count > i);
                }
            }
        }

        private void OnEnable()
        {
            if (_Interactable != null)
            {
                _Interactable.hoverEntered.AddListener(OnHoverEntered);
                _Interactable.hoverExited.AddListener(OnHoverExited);
                
                _currentInteractors.Clear();
                foreach (var interactor in _Interactable.interactorsHovering)
                    _currentInteractors.Add(interactor);
            }
            _AvailableTargetPoints.ForEach(point => point.gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            if (_Interactable != null)
            {
                _Interactable.hoverEntered.RemoveListener(OnHoverEntered);
                _Interactable.hoverExited.RemoveListener(OnHoverExited);
            }
            _currentInteractors.Clear();
        }

        private void LateUpdate()
        {
            if (_Collider == null)
                return;

            _currentPositions.Clear();
            
            var pointIndex = 0;
            for (var i = 0; i < _currentInteractors.Count && pointIndex < _AvailableTargetPoints.Count; i++)
            {
                if (!_currentInteractors[i].TryGetCurrentRaycastTarget(_Collider, out var hit))
                    continue;

                _currentPositions.Add(hit.point);

                var targetPoint = _AvailableTargetPoints[pointIndex];
                targetPoint.position = hit.point + transform.TransformDirection(_SurfaceOffset);
                targetPoint.gameObject.SetActive(_ShowTargetPoints);
                pointIndex++;
            }
            for (var i = pointIndex; i < _AvailableTargetPoints.Count; i++)
            {
                _AvailableTargetPoints[i].gameObject.SetActive(false);
            }
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (!_currentInteractors.Contains(args.interactorObject))
                _currentInteractors.Add(args.interactorObject);
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            _currentInteractors.Remove(args.interactorObject);
        }
    }
}