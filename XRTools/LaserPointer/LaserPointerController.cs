// Author: Michal Petr
// Created: 20.10.2026

using System;
using EDIVE.Core.Services;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.XRTools.LaserPointer
{
    public class LaserPointerController : AServiceBehaviour<LaserPointerController>
    {
        [SerializeField]
        private LayerMask _HitMask = ~0;

        [SerializeField]
        private float _MaxDistance = 30f;

        [SerializeField]
        private Hand _LeftHand;

        [SerializeField]
        private Hand _RightHand;

        public InteractorHandedness ActiveHand { get; private set; }
        public bool IsActive => ActiveHand != InteractorHandedness.None;
        public bool IsFiring { get; private set; }

        public Transform Anchor => GetHand(ActiveHand)?.Anchor;
        public Vector3 LocalOrigin { get; private set; }
        public Vector3 EndPoint { get; private set; }
        public bool HasHit { get; private set; }

        public event Action<InteractorHandedness> ActiveHandChanged;
        public event Action<bool> FiringChanged;
        public event Action BeamUpdated;

        protected override void OnDisable()
        {
            SetActiveHand(InteractorHandedness.None);
            base.OnDisable();
        }

        public void SetActiveHand(InteractorHandedness hand)
        {
            if (ActiveHand == hand)
                return;

            SetFiring(false);
            ActiveHand = hand;
            _LeftHand.SetActive(hand == InteractorHandedness.Left);
            _RightHand.SetActive(hand == InteractorHandedness.Right);
            ActiveHandChanged?.Invoke(hand);
        }

        public void SetFiring(bool firing)
        {
            firing &= IsActive && GetHand(ActiveHand).CastOrigin != null;
            if (IsFiring == firing)
                return;

            IsFiring = firing;
            if (IsFiring)
                UpdateBeam();

            FiringChanged?.Invoke(IsFiring);
        }

        private void Update()
        {
            if (IsFiring)
                UpdateBeam();
        }

        private void UpdateBeam()
        {
            var hand = GetHand(ActiveHand);
            var castOrigin = hand.CastOrigin;
            var origin = castOrigin.position;
            var direction = castOrigin.forward;
            HasHit = Physics.Raycast(origin, direction, out var hit, _MaxDistance, _HitMask, QueryTriggerInteraction.Ignore);
            EndPoint = HasHit ? hit.point : origin + direction * _MaxDistance;
            LocalOrigin = hand.Anchor.InverseTransformPoint(origin);
            BeamUpdated?.Invoke();
        }

        private Hand GetHand(InteractorHandedness hand) => hand switch
        {
            InteractorHandedness.Left => _LeftHand,
            InteractorHandedness.Right => _RightHand,
            _ => null
        };

        [Serializable]
        private class Hand
        {
            [SerializeField]
            [Tooltip("Ray starts at this interactor's far cast origin.")]
            private NearFarInteractor _Interactor;

            [SerializeField]
            [Tooltip("Transform the avatar hand follows. Beam origin is sent relative to it.")]
            private Transform _Anchor;

            [SerializeField]
            private AToggleState _ActiveVisual;

            public Transform Anchor => _Anchor;
            public Transform CastOrigin => _Interactor != null && _Anchor != null && _Interactor.farInteractionCaster is { } caster ? caster.castOrigin : null;

            public void SetActive(bool active)
            {
                if (_ActiveVisual)
                    _ActiveVisual.SetState(active);
            }
        }
    }
}
