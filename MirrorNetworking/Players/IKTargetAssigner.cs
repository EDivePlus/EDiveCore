using UnityEngine;

public class IKTargetAssigner : MonoBehaviour
{
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _leftHand;
    [SerializeField] private Transform _rightHand;

    public void Assign(GameObject avatar)
    {
        Debug.Log("Assign() called!");

        var rig = avatar.GetComponentInChildren<IKTargetFollowVRRig>();
        if (rig == null)
        {
            Debug.LogError("IKTargetFollowVRRig NOT found in avatar.");
            return;
        }

        Debug.Log("IKTargetFollowVRRig found, assigning...");

        rig.head.vrTarget = _head;
        rig.leftHand.vrTarget = _leftHand;
        rig.rightHand.vrTarget = _rightHand;

        Debug.Log("IKTargetAssigner: Assigned skeleton transforms to avatar rig.");
    }
}
