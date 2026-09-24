using UnityEngine;

namespace EDIVE.Rendering.Mirrors
{
    // A box a mirror may see into. Trims its culling so the reflection stops at the room.
    [DisallowMultipleComponent]
    public class MirrorVisibilityVolume : MonoBehaviour
    {
        [SerializeField]
        private Vector3 _Center;

        [SerializeField]
        private Vector3 _Size = new(10f, 4f, 10f);

        private readonly Vector3[] _corners = new Vector3[8];

        public Vector3 Center => _Center;
        public Vector3 Size => _Size;

        // Reused between calls. Read it before asking another volume.
        public Vector3[] GetWorldCorners()
        {
            var min = _Center - _Size * 0.5f;
            var max = _Center + _Size * 0.5f;
            var matrix = transform.localToWorldMatrix;

            for (var i = 0; i < 8; i++)
            {
                _corners[i] = matrix.MultiplyPoint3x4(new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z));
            }

            return _corners;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.2f);
            Gizmos.DrawCube(_Center, _Size);
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(_Center, _Size);
        }
    }
}
