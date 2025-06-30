using EDIVE.OdinExtensions.Attributes;
using Mirror;
using UnityEngine;

namespace EDIVE.MirrorNetworking.Players
{
    public class SelfVisibility : MonoBehaviour
    {
        [SerializeField]
        [LayerField]
        private int _LocalOnlyLayer;

        private void Start()
        {
            var net = GetComponentInParent<NetworkIdentity>();
            if (net == null || !net.isLocalPlayer)
            {
                Destroy(this);
                return;
            }

            if (_LocalOnlyLayer == -1)
            {
                Debug.LogError($"Layer '{_LocalOnlyLayer}' does not exist.");
                return;
            }

            foreach (var child in GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = _LocalOnlyLayer;
            }
        }
    }
}
