// Author: Michal Petr
// Created: 20.10.2026

using System;
using PurrNet.Packing;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.Networking.XR
{
    [Serializable]
    public struct LaserPointerBeamState : IPackedAuto, IEquatable<LaserPointerBeamState>
    {
        [SerializeField]
        private byte _Hand;

        [SerializeField]
        private Vector3 _LocalOrigin;

        [SerializeField]
        private Vector3 _EndPoint;

        [SerializeField]
        private bool _HasHit;

        public InteractorHandedness Hand => (InteractorHandedness) _Hand;
        public Vector3 LocalOrigin => _LocalOrigin;
        public Vector3 EndPoint => _EndPoint;
        public bool HasHit => _HasHit;

        public LaserPointerBeamState(InteractorHandedness hand, Vector3 localOrigin, Vector3 endPoint, bool hasHit)
        {
            _Hand = (byte) hand;
            _LocalOrigin = localOrigin;
            _EndPoint = endPoint;
            _HasHit = hasHit;
        }

        public bool Equals(LaserPointerBeamState other) => _Hand == other._Hand && _LocalOrigin == other._LocalOrigin && _EndPoint == other._EndPoint && _HasHit == other._HasHit;
        public override bool Equals(object obj) => obj is LaserPointerBeamState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_Hand, _LocalOrigin, _EndPoint, _HasHit);
    }
}
