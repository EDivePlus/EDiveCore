using System;
using UnityEngine;

namespace EDIVE.Avatars
{
    public class IKTargetRigFollow : ARigFollow
    {
        [Range(0,1)]
        [SerializeField]
        private float _TurnSmoothness = 0.1f;

        [SerializeField]
        private FollowRecord _Head;

        [SerializeField]
        private FollowRecord _LeftHand;

        [SerializeField]
        private FollowRecord _RightHand;

        [SerializeField]
        private Vector3 _HeadBodyPositionOffset;

        public override Transform HeadSource { get => _Head.Source; set => _Head.Source = value; }
        public override Transform LeftHandSource { get => _LeftHand.Source; set => _LeftHand.Source = value; }
        public override Transform RightHandSource { get => _RightHand.Source; set => _RightHand.Source = value; }

        private void LateUpdate()
        {
            if (!_Head.IsValid)
                return;

            transform.position = _Head.Target.position + _HeadBodyPositionOffset;
            var yaw = _Head.Source.eulerAngles.y;
            transform.rotation = Quaternion.Lerp(transform.rotation,Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z),_TurnSmoothness);

            _Head.Map();
            _LeftHand.Map();
            _RightHand.Map();
        }

        [Serializable]
        public class FollowRecord
        {
            [SerializeField]
            private Transform _Source;

            [SerializeField]
            private Transform _Target;

            [SerializeField]
            private Vector3 _TrackingPositionOffset;

            [SerializeField]
            private Vector3 _TrackingRotationOffset;

            public Transform Target => _Target;
            public Transform Source
            {
                get => _Source;
                set => _Source = value;
            }

            public bool IsValid => _Source != null && _Target != null;

            public void Map()
            {
                if (!IsValid) return;
                _Target.position = _Source.TransformPoint(_TrackingPositionOffset);
                _Target.rotation = _Source.rotation * Quaternion.Euler(_TrackingRotationOffset);
            }
        }
    }
}
