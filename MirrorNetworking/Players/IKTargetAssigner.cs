using EDIVE.DataStructures.ScriptableVariables;
using EDIVE.DataStructures.ScriptableVariables.Variables;
using Mirror;
using UnityEngine;

namespace EDIVE.MirrorNetworking.Players
{
    public class IKTargetAssigner : NetworkBehaviour
    {
        [SerializeField]
        private IKTargetFollowVRRig _TargetFollow;
        
        [SerializeField]
        private TransformScriptableVariable _Head;

        [SerializeField]
        private TransformScriptableVariable _LeftHand;

        [SerializeField]
        private TransformScriptableVariable _RightHand;
        
        public override void OnStartLocalPlayer()
        {
            TryAssignIKTargets();
        }
        public override void OnStartClient()
        {
            Debug.LogError("pROSIMM");
        }
    
        private void TryAssignIKTargets()
        {
            if (_TargetFollow == null)
            {
                Debug.LogWarning("IKTargetFollowVRRig not found in avatar prefab.");
                return;
            }

            _TargetFollow.head.vrTarget = _Head;
            _TargetFollow.leftHand.vrTarget = _LeftHand;
            _TargetFollow.rightHand.vrTarget = _RightHand;

            _Head.ValueChanged.AddListener(() => _TargetFollow.head.vrTarget = _Head);
            _LeftHand.ValueChanged.AddListener(() => _TargetFollow.leftHand.vrTarget = _LeftHand);
            _RightHand.ValueChanged.AddListener(() => _TargetFollow.rightHand.vrTarget = _RightHand);


            Debug.Log("IK rig successfully assigned to XR targets.");
        }
    }
    
}
