using System;
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

        public void Assign(GameObject avatar)
        {
            if (isLocalPlayer)
            {
                var headFollow = _HeadTarget.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                headFollow.Source = _HeadTarget.RigTarget;

                var leftHandFollow = _RightHandTarget.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                leftHandFollow.Source = _RightHandTarget.RigTarget;

                var rightHandFollow = _RightHandTarget.SkeletonTarget.gameObject.AddComponent<ScriptableTransformFollow>();
                rightHandFollow.Source = _RightHandTarget.RigTarget;
            }

            var rig = avatar.GetComponentInChildren<IKTargetFollowVRRig>();
            if (!rig)
            {
                Debug.LogError("IKTargetFollowVRRig NOT found in avatar.");
                return;
            }

            rig.head.vrTarget = _HeadTarget.SkeletonTarget;
            rig.leftHand.vrTarget = _LeftHandTarget.SkeletonTarget;
            rig.rightHand.vrTarget = _RightHandTarget.SkeletonTarget;
        }
    }
}
