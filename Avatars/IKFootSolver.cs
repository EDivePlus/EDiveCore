// Author: František Holubec
// Created: 14.10.2025

using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Avatars
{
    [DefaultExecutionOrder(10)]
    public class IKFootSolver : MonoBehaviour
    {
        [Title("References", HorizontalLine = false)]
        [SerializeField]
        [Tooltip("Mask defining walkable surfaces, used for ray-casting")]
        private LayerMask _WalkableLayer = 1;
        
        [SerializeField]
        [Tooltip("Body transform for movement tracking")]
        private Transform _Body;
        
        [SerializeField]
        [Tooltip("Reference to the other foot IK solver to prevent both feet moving at the same time")]
        private IKFootSolver _OtherFoot;
        
        [Title("Step Parameters", HorizontalLine = false)]
        [SerializeField] 
        [Tooltip("Speed of the step animation in seconds")]
        private float _StepAnimationSpeed = 5;
        
        [SerializeField] 
        [Tooltip("Range of body speeds influencing step behavior")]
        private Vector2 _BodySpeedRange = new(0.1f, 2f);
        
        [SerializeField]
        [Tooltip("Time taken to smooth body speed changes")]
        public float _SpeedSmoothTime = 0.4f;
        
        [SerializeField] 
        [Tooltip("Distance it takes to trigger a step, relative to body speed")]
        private Vector2 _StepDistanceThresholdRange = new(0.1f, 0.75f);
        
        [SerializeField]
        [Tooltip("Rotation angle change in degrees required to trigger a step")]
        private float _StepRotationThreshold = 45f;
        
        [SerializeField]
        [Tooltip("Minimum body speed required to consider stepping forward")]
        private float _MinSpeedForStep = 0.1f;
        
        [SerializeField] 
        [Tooltip("Length of a predicted step, relative to body speed. Should be always less than step distance threshold.")]
        private Vector2 _StepLengthRange = new(0.08f, 0.7f);
        
        [SerializeField] 
        [Tooltip("Height of a step, relative to body speed.")]
        private Vector2 _StepHeightRange = new(0.1f, 0.3f);
        
        [SerializeField]
        [Tooltip("Distance threshold for instant teleporting the foot to the new position")]
        private float _TeleportDistanceThreshold = 2f;
        
        [SerializeField]
        [Tooltip("Offset added to foot Y position to avoid clipping with the ground")]
        public float _FootYPosOffset = 0.1f;
        
        [SerializeField]
        [Tooltip("Offset applied to foot position when no ground is detected")]
        public Vector3 _OffGroundPosition = new(0, -1f, 0);
        
        [SerializeField]
        [Tooltip("Smooth time for foot movement when off the ground")]
        public float _OffGroundSmoothTime = 0.1f;
        
        [Title("Raycast settings", HorizontalLine = false)]
        [SerializeField]
        [Tooltip("Vertical offset for the raycast start position")]
        public float _RayStartYOffset;
        
        [SerializeField]
        [Tooltip("Length of the raycast used to detect the ground")]
        public float _RayLength = 1.5f;

        private bool IsMoving => _lerp < 1;
        
        private Vector3 _oldPosition;
        private Vector3 _currentPosition;
        private Vector3 _newPosition;
        
        private Vector3 _oldNormal;
        private Vector3 _currentNormal;
        private Vector3 _newNormal;
        
        private float _footSpacing;
        private float _lerp;
        
        private Quaternion _initialLocalRotation;
        private Vector3 _lastStepForward;
        private Vector3 _lastBodyPosition;

        private float _smoothBodySpeed;
        private float _smoothBodyVelocity;
        
        private Vector3 _smoothOffGroundVelocity;
        
        private readonly RaycastHit[] _rayHits = new RaycastHit[1];

        private void Start()
        {
            _footSpacing = transform.localPosition.x;
            _currentPosition = _newPosition = _oldPosition = transform.position;
            _currentNormal = Vector3.up;
            _initialLocalRotation = transform.localRotation;
            _lerp = 1;
            
            _lastStepForward = Vector3.ProjectOnPlane(_Body.forward, Vector3.up).normalized;
            _lastBodyPosition = _Body.position;
        }

        private void Update()
        {
            // Calculate body speed for dynamic scaling
            var bodyDelta = _Body.position - _lastBodyPosition;
            var bodySpeed = bodyDelta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            _smoothBodySpeed = Mathf.SmoothDamp(_smoothBodySpeed, bodySpeed, ref _smoothBodyVelocity, _SpeedSmoothTime);
            _lastBodyPosition = _Body.position;

            // Adjust step parameters based on speed
            var speedRatio = Mathf.Clamp01(Mathf.InverseLerp(_BodySpeedRange.x, _BodySpeedRange.y, _smoothBodySpeed));
            var stepLength = Mathf.Lerp(_StepLengthRange.x, _StepLengthRange.y, speedRatio);
            var stepHeight = Mathf.Lerp(_StepHeightRange.x, _StepHeightRange.y, speedRatio);
            var stepDistanceThreshold = Mathf.Lerp(_StepDistanceThresholdRange.x, _StepDistanceThresholdRange.y, speedRatio);
            
            var bodyForward = Vector3.ProjectOnPlane(_Body.forward, Vector3.up).normalized;
            var bodyAttachPosition = _Body.position + (_Body.right * _footSpacing);
            
            // Raycast to find ground position
            var rayStart = bodyAttachPosition + Vector3.up * _RayStartYOffset;
            var ray = new Ray(rayStart, Vector3.down);
            if (Physics.RaycastNonAlloc(ray, _rayHits,  _RayLength, _WalkableLayer.value) > 0)
            {
                var rayHit = _rayHits[0];
                var distanceStep = Vector3.Distance(_newPosition, rayHit.point) > stepDistanceThreshold;
                var rotationStep = Vector3.Angle(_lastStepForward, bodyForward) >= _StepRotationThreshold;

                // Step decision
                if ((distanceStep || rotationStep) && !_OtherFoot.IsMoving && _lerp >= 1)
                {
                    var targetPosition = rayHit.point;
                    if (distanceStep && bodySpeed > _MinSpeedForStep)
                    {
                        var direction = (rayHit.point - _currentPosition).WithY(0).normalized;
                        targetPosition += direction * stepLength;
                    }
                    
                    // Check for teleportation
                    if (Vector3.Distance(_currentPosition, targetPosition) > _TeleportDistanceThreshold)
                    {
                        _lerp = 1;
                        _newPosition = _oldPosition = _currentPosition = targetPosition;
                        _newNormal = _oldNormal = _currentNormal = rayHit.normal;
                        _smoothBodyVelocity = 0;
                        _smoothBodySpeed = 0;
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
            else
            {
                var targetPosition = bodyAttachPosition + _OffGroundPosition;
                
                // Check for teleportation
                if (Vector3.Distance(_currentPosition, targetPosition) > _TeleportDistanceThreshold)
                {
                    _newPosition = _oldPosition = _currentPosition = targetPosition;
                    _smoothOffGroundVelocity = Vector3.zero;
                }
                else
                {
                    _newPosition = _oldPosition = _currentPosition = Vector3.SmoothDamp(_currentPosition, targetPosition, ref _smoothOffGroundVelocity, _OffGroundSmoothTime);
                }
                
                _newNormal = _oldNormal = _currentNormal = Vector3.up;
                _smoothBodyVelocity = 0;
                _smoothBodySpeed = 0;
                _lerp = 1;
            }

            // Step animation
            if (_lerp < 1)
            {
                var tempPosition = Vector3.Lerp(_oldPosition, _newPosition, _lerp);
                tempPosition.y += Mathf.Sin(_lerp * Mathf.PI) * stepHeight;

                _currentPosition = tempPosition;
                _currentNormal = Vector3.Lerp(_oldNormal, _newNormal, _lerp);
                _lerp += Time.deltaTime * _StepAnimationSpeed;
            }
            else
            {
                _oldPosition = _newPosition;
                _oldNormal = _newNormal;
            }
            
            // Apply position and rotation
            transform.position = _currentPosition + Vector3.up * _FootYPosOffset;
            transform.rotation =  Quaternion.LookRotation(bodyForward, _currentNormal) * _initialLocalRotation;
        }
        
        private void OnDrawGizmos()
        {
            if (_Body == null)
                return;
            
            var speedRatio = Mathf.Clamp01(Mathf.InverseLerp(_BodySpeedRange.x, _BodySpeedRange.y, _smoothBodySpeed));
            var stepDistanceThreshold = Mathf.Lerp(_StepDistanceThresholdRange.x, _StepDistanceThresholdRange.y, speedRatio);
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.9f);
            Gizmos.DrawWireSphere(_newPosition, stepDistanceThreshold);
            Gizmos.color = new Color(0f, 1f, 0f, 0.9f);
            Gizmos.DrawSphere(_currentPosition, 0.05f);
            Gizmos.color = new Color(0f, 0f, 1f, 0.9f);
            Gizmos.DrawSphere(_rayHits[0].point, 0.05f);
            
            var rayStart = _Body.position + (_Body.right * _footSpacing) + Vector3.up * _RayStartYOffset;
            var rayEnd = rayStart + Vector3.down * _RayLength;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rayStart, rayEnd);
        }
    }
}
