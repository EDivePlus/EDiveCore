// Author: Radim Holub // Created: 18.03.2026

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using Vector3 = UnityEngine.Vector3;

namespace EDIVE.XRTools.Utils
{
    [DisallowMultipleComponent]
    public class InteractionPositionSampler : MonoBehaviour
    {
        [SerializeField]
        private bool _ShowTargetPoints = true;
        
        [SerializeField]
        private Vector3 _SurfaceOffset = Vector3.zero;
        
        [Required]
        [SerializeField] 
        private XRSimpleInteractable _Interactable;

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
        }

        private void LateUpdate()
        {
            if (_Collider == null)
                return;
            
            _currentPositions.Clear();
            for (var i = 0; i < _AvailableTargetPoints.Count; i++)
            {
                var targetPoint = _AvailableTargetPoints[i];
                if (i < _currentInteractors.Count && TryGetHoveringHit(_currentInteractors[i], out var hit))
                {
                    _currentPositions.Add(hit.point);
                    targetPoint.position = hit.point + transform.TransformDirection(_SurfaceOffset);
                    if (ShowTargetPoints)
                        targetPoint.gameObject.SetActive(true);
                }
                else
                {
                    targetPoint.gameObject.SetActive(false);
                }
            }
        }
        
        private bool TryGetHoveringHit(IXRHoverInteractor interactor, out RaycastHit hit)
        {
            hit = default;
            
            if (interactor is XRRayInteractor rayInteractor)
            {
                if (rayInteractor.TryGetCurrent3DRaycastHit(out hit))
                    return hit.collider == _Collider;
            }

            if (interactor is NearFarInteractor nearFarInteractor)
            {
                if (nearFarInteractor.TryGetCurveEndPoint(out var endPoint) == EndPointType.ValidCastHit)
                {
                    var source = nearFarInteractor.attachTransform != null
                        ? nearFarInteractor.attachTransform
                        : nearFarInteractor.transform;

                    var rayDirection = source.forward;
                    if (rayDirection.sqrMagnitude < 0.0001f)
                        return false;

                    rayDirection.Normalize();

                    var rayOrigin = endPoint - rayDirection * 0.1f;
                    return _Collider.Raycast(new Ray(rayOrigin, rayDirection), out hit, 0.2f);
                }
            }

            return false;
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            _currentInteractors.Add(args.interactorObject);
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            _currentInteractors.Remove(args.interactorObject);
        }
    }
}