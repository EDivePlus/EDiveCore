using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.Avatars
{
    [DefaultExecutionOrder(10)]
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
        
        [SerializeField]
        private float _Speed = 5;
        
        [SerializeField]
        private float _StepDistanceThreshold = 0.3f;
        
        [SerializeField]
        private float _StepRotationThreshold = 90f;
        
        [SerializeField]
        private float _TeleportDistanceThreshold = 1f;
        
        [SerializeField]
        private float _StepLength = 0.3f;
        
        [SerializeField]
        private float _SideStepLength = 0.1f;
        
        [SerializeField]
        private float _StepHeight = 0.3f;

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

        [SerializeField]
        private float _footSpacing;
        private Vector3 _oldPosition;
        private Vector3 _currentPosition;
        private Vector3 _newPosition;
        private Vector3 _oldNormal;
        private Vector3 _currentNormal;
        private Vector3 _newNormal;
        private float _lerp;
        
        private Quaternion _initialLocalRotation;
        private Vector3 _lastStepForward;
        
        private readonly RaycastHit[] _rayHits = new RaycastHit[1];

        private void Start()
        {
            _footSpacing = transform.localPosition.x;
            _currentPosition = _newPosition = _oldPosition = transform.position;
            _currentNormal = Vector3.up;
            _initialLocalRotation = transform.localRotation;
            _lerp = 1;
            
            _lastStepForward = Vector3.ProjectOnPlane(_Body.forward, Vector3.up).normalized;
        }

        private void Update()
        {
            var rayStart = _Body.position + (_Body.right * _footSpacing) + Vector3.up * _RayStartYOffset;
            var ray = new Ray(rayStart, Vector3.down);
            var bodyForward = Vector3.ProjectOnPlane(_Body.forward, Vector3.up).normalized;

            if (Physics.RaycastNonAlloc(ray, _rayHits,  _RayLength, _TerrainLayer.value) > 0)
            {
                var rayHit = _rayHits[0];
                var distanceStep = Vector3.Distance(_newPosition, rayHit.point) > _StepDistanceThreshold;
                var rotationStep = Vector3.Angle(_lastStepForward, bodyForward) >= _StepRotationThreshold;

                if ((distanceStep || rotationStep) && !_OtherFoot.IsMoving && _lerp >= 1)
                {
                    var direction = Vector3.ProjectOnPlane(rayHit.point - _currentPosition, Vector3.up).normalized;
                    var angle = Vector3.Angle(_Body.forward, _Body.InverseTransformDirection(direction));
                    var stepLength = angle is < 50 or > 130 ? _StepLength : _SideStepLength;
                    if (!distanceStep)
                        stepLength = 0;
                        
                    var targetPosition = rayHit.point + direction * stepLength;
                    
                    if (Vector3.Distance(_currentPosition, targetPosition) > _TeleportDistanceThreshold)
                    {
                        _lerp = 1;
                        _newPosition = _oldPosition = _currentPosition = targetPosition;
                        _newNormal = _oldNormal = _currentNormal = rayHit.normal;
                    }
                    else
                    {
                        _lerp = 0;
                        _newPosition = targetPosition;
                        _newNormal = rayHit.normal;
                    }

                    _lastStepForward = bodyForward;
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
            
            transform.position = _currentPosition + Vector3.up * _FootYPosOffset;
            transform.rotation =  Quaternion.LookRotation(bodyForward, _currentNormal) * _initialLocalRotation;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_newPosition, _StepDistanceThreshold);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_currentPosition, 0.1f);
            Gizmos.color = Color.blue;
            var rayHit = _rayHits[0];
            Gizmos.DrawSphere(rayHit.point, 0.1f);
        }
    }
}
