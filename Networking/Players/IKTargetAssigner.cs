using System;
using System.Collections.Generic;
using EDIVE.Avatars;
using EDIVE.ScriptableArchitecture.Variables;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using PurrNet;
using UnityEngine;

namespace EDIVE.Networking.Players
{
    public class IKTargetAssigner : NetworkBehaviour
    {
        [SerializeField]
        private IKTargetRecord _HeadTarget;

        [SerializeField]
        private IKTargetRecord _LeftHandTarget;

        [SerializeField]
        private IKTargetRecord _RightHandTarget;
        
        [SerializeField]
        private List<IKTargetRecord> _OtherTargets = new();

        private readonly SyncVar<bool> _leftHandTracked = new(ownerAuth: true);
        private readonly SyncVar<bool> _rightHandTracked = new(ownerAuth: true);

        private AvatarController _avatar;

        [Serializable]
        public class IKTargetRecord
        {
            [SerializeField]
            private TransformScriptableVariable _RigTarget;

            [SerializeField]
            private Transform _SkeletonTarget;

            public TransformScriptableVariable RigTarget => _RigTarget;
            public Transform SkeletonTarget => _SkeletonTarget;

            public bool IsRigTargetActive()
            {
                var rigTarget = _RigTarget != null ? _RigTarget.Value : null;
                return rigTarget != null && rigTarget.gameObject.activeInHierarchy;
            }
        }

        private void Awake()
        {
            _leftHandTracked.onChanged += OnHandTrackedChanged;
            _rightHandTracked.onChanged += OnHandTrackedChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _leftHandTracked.onChanged -= OnHandTrackedChanged;
            _rightHandTracked.onChanged -= OnHandTrackedChanged;
        }

        // Desktop controls have no hands, untracked hands fall back to animation
        private void Update()
        {
            if (!isSpawned || !isOwner)
                return;

            var leftTracked = _LeftHandTarget.IsRigTargetActive();
            if (_leftHandTracked.value != leftTracked)
                _leftHandTracked.value = leftTracked;

            var rightTracked = _RightHandTarget.IsRigTargetActive();
            if (_rightHandTracked.value != rightTracked)
                _rightHandTracked.value = rightTracked;
        }

        private void OnHandTrackedChanged(bool tracked) => ApplyHandSources();

        protected override void OnSpawned(bool asServer)
        {
            if (asServer) return;
            if (isOwner)
            {
                var headFollow = _HeadTarget.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                headFollow.Source = _HeadTarget.RigTarget;

                var leftHandFollow = _LeftHandTarget.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                leftHandFollow.Source = _LeftHandTarget.RigTarget;

                var rightHandFollow = _RightHandTarget.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                rightHandFollow.Source = _RightHandTarget.RigTarget;
                
                foreach (var record in _OtherTargets)
                {
                    var follow = record.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                    follow.Source = record.RigTarget;
                }
            }
        }
        
        public void Assign(AvatarController avatar)
        {
            _avatar = avatar;
            if (avatar == null)
                return;

            if (avatar.RigFollow)
            {
                avatar.RigFollow.HeadSource = _HeadTarget.SkeletonTarget;
                ApplyHandSources();
                avatar.RigFollow.Refresh();
            }
        }

        private void ApplyHandSources()
        {
            if (_avatar == null || !_avatar.RigFollow)
                return;

            _avatar.RigFollow.LeftHandSource = _leftHandTracked.value ? _LeftHandTarget.SkeletonTarget : null;
            _avatar.RigFollow.RightHandSource = _rightHandTracked.value ? _RightHandTarget.SkeletonTarget : null;
        }
    }
}
