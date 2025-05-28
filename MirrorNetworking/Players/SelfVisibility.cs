using UnityEngine;
using Mirror;

public class SelfVisibility : MonoBehaviour
{
    [SerializeField] private string _LocalOnlyLayer = "Player";
    
    private void Start()
    {
        var net = GetComponentInParent<NetworkIdentity>();
        if (net == null || !net.isLocalPlayer)
        {
            Destroy(this);
            return;
        }

        int layer = LayerMask.NameToLayer(_LocalOnlyLayer);
        if (layer == -1)
        {
            Debug.LogError($"Layer '{_LocalOnlyLayer}' does not exist.");
            return;
        }
        
        SetLayerRecursively(transform, layer);
    }

    private void SetLayerRecursively(Transform obj, int layer)
    {
        obj.gameObject.layer = layer;
        foreach (Transform child in obj)
        {
            SetLayerRecursively(child, layer);
        }
    }
}
