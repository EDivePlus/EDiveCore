using UnityEngine;

namespace EDIVE.Utils
{
    [RequireComponent(typeof(Collider))]
    public class BoundingBoxDisplay : MonoBehaviour
    {
        private Collider _backingCollider;
        public Collider Collider => _backingCollider ??= GetComponent<Collider>();

        void OnDrawGizmos()
        {

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Collider.bounds.center, Collider.bounds.size);
        }
    }
}
