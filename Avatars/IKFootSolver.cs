using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.Avatars
{
    public class IKFootSolver : MonoBehaviour
    {
        [FormerlySerializedAs("terrainLayer")]
        [SerializeField]
        private LayerMask _TerrainLayer;

        [FormerlySerializedAs("body")]
        [SerializeField]
        private Transform _Body;

        [FormerlySerializedAs("otherFoot")]
        [SerializeField]
        private IKFootSolver _OtherFoot;

        [FormerlySerializedAs("speed")]
        [SerializeField]
        private float _Speed = 4;

        [FormerlySerializedAs("stepDistance")]
        [SerializeField]
        private float _StepDistance = .2f;

        [FormerlySerializedAs("stepLength")]
        [SerializeField]
        private float _StepLength = .2f;

        [FormerlySerializedAs("sideStepLength")]
        [SerializeField]
        private float _SideStepLength = .1f;

        [FormerlySerializedAs("stepHeight")]
        [SerializeField]
        private float _StepHeight = .3f;

        [FormerlySerializedAs("footOffset")]
        [SerializeField]
        private Vector3 _FootOffset;

        [FormerlySerializedAs("footRotOffset")]
        [SerializeField]
        public Vector3 _FootRotOffset;

        [FormerlySerializedAs("footYPosOffset")]
        [SerializeField]
        public float _FootYPosOffset = 0.1f;

        [FormerlySerializedAs("rayStartYOffset")]
        [SerializeField]
        public float _RayStartYOffset;

        [FormerlySerializedAs("rayLength")]
        [SerializeField]
        public float _RayLength = 1.5f;

        public bool IsMoving => _lerp < 1;

        private float _footSpacing;
        private Vector3 _oldPosition;
        private Vector3 _currentPosition;
        private Vector3 _newPosition;
        private Vector3 _oldNormal;
        private Vector3 _currentNormal;
        private Vector3 _newNormal;
        private float _lerp;
        
        private readonly RaycastHit[] _rayHits = new RaycastHit[1];

        private void Start()
        {
            _footSpacing = transform.localPosition.x;
            _currentPosition = _newPosition = _oldPosition = transform.position;
            _currentNormal = _newNormal = _oldNormal = transform.up;
            _lerp = 1;
        }

        private void Update()
        {
            transform.position = _currentPosition + Vector3.up * _FootYPosOffset;
            // Todo fix rotation by initial rotation
            //transform.up = _currentNormal; 
            //transform.localRotation = Quaternion.Euler(_FootRotOffset);

            var rayStart = _Body.position + (_Body.right * _footSpacing) + Vector3.up * _RayStartYOffset;
            var ray = new Ray(rayStart, Vector3.down);

            // Debug.DrawRay(rayStart, Vector3.down);
            if (Physics.RaycastNonAlloc(ray, _rayHits,  _RayLength, _TerrainLayer.value) > 0)
            {
                var rayHit = _rayHits[0];
                if (Vector3.Distance(_newPosition, rayHit.point) > _StepDistance && !_OtherFoot.IsMoving && _lerp >= 1)
                {
                    _lerp = 0;
                    var direction = Vector3.ProjectOnPlane(rayHit.point - _currentPosition, Vector3.up).normalized;
                    var angle = Vector3.Angle(_Body.forward, _Body.InverseTransformDirection(direction));
                    
                    var stepLength = angle is < 50 or > 130 ? _StepLength : _SideStepLength;
                    _newPosition = rayHit.point + direction * stepLength + _FootOffset;
                    _newNormal = rayHit.normal;
                }
            }

            if (_lerp < 1)
            {
                var tempPosition = Vector3.Lerp(_oldPosition, _newPosition, _lerp);
                tempPosition.y += Mathf.Sin(_lerp * Mathf.PI) * _StepHeight;

                _currentPosition = tempPosition;
                _currentNormal = Vector3.Lerp(_oldNormal, _newNormal, _lerp);
                _lerp += Time.deltaTime * _Speed;
            }
            else
            {
                _oldPosition = _newPosition;
                _oldNormal = _newNormal;
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_newPosition, _StepDistance);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_currentPosition, 0.1f);
            Gizmos.color = Color.blue;
            var rayHit = _rayHits[0];
            Gizmos.DrawSphere(rayHit.point, 0.1f);
        }
    }
}
