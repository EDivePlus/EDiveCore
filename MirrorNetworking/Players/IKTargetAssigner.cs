using System;
using EDIVE.Avatars;
using EDIVE.DataStructures.ScriptableVariables;
using EDIVE.DataStructures.ScriptableVariables.Variables;
using Mirror;
using UnityEngine;

namespace EDIVE.MirrorNetworking.Players
{
    public class IKTargetAssigner : NetworkBehaviour
    {
        [SerializeField]
        private IKTargetRecord _HeadTarget;

        [SerializeField]
        private IKTargetRecord _LeftHandTarget;

        [SerializeField]
        private IKTargetRecord _RightHandTarget;

        [Serializable]
        private class IKTargetRecord
        {
            [SerializeField]
            private TransformScriptableVariable _RigTarget;

            [SerializeField]
            private Transform _SkeletonTarget;

            public TransformScriptableVariable RigTarget => _RigTarget;
            public Transform SkeletonTarget => _SkeletonTarget;
        }

        public void InitializeFollow()
        {
            if (isLocalPlayer)
            {
                var headFollow = _HeadTarget.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                headFollow.Source = _HeadTarget.RigTarget;

                var leftHandFollow = _LeftHandTarget.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                leftHandFollow.Source = _LeftHandTarget.RigTarget;

                var rightHandFollow = _RightHandTarget.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                rightHandFollow.Source = _RightHandTarget.RigTarget;
            }
        }

        public void Assign(AvatarController avatar)
        {
            if (avatar == null)
                return;

            if (avatar.RigFollow)
            {
                avatar.RigFollow.HeadSource = _HeadTarget.SkeletonTarget;
                avatar.RigFollow.LeftHandSource = _LeftHandTarget.SkeletonTarget;
                avatar.RigFollow.RightHandSource = _RightHandTarget.SkeletonTarget;
            }
        }
    }
}
