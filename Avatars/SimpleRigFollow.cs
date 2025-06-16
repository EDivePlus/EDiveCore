// Author: František Holubec
// Created: 16.06.2025

using System;
using UnityEngine;

namespace EDIVE.Avatars
{
    public class SimpleRigFollow : ARigFollow
    {
        [SerializeField]
        private FollowRecord _Head;

        [SerializeField]
        private FollowRecord _LeftHand;

        [SerializeField]
        private FollowRecord _RightHand;

        public override Transform HeadSource { get => _Head.Source; set => _Head.Source = value; }
        public override Transform LeftHandSource { get => _LeftHand.Source; set => _LeftHand.Source = value; }
        public override Transform RightHandSource { get => _RightHand.Source; set => _RightHand.Source = value; }

        private void LateUpdate()
        {
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

            public Transform Source
            {
                get => _Source;
                set => _Source = value;
            }

            public bool IsValid => _Source != null && _Target != null;

            public void Map()
            {
                if (!IsValid) return;
                _Target.position = _Source.position;
                _Target.rotation = _Source.rotation;
            }
        }
    }
}
