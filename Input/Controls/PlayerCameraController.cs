// Author: František Holubec
// Created: 2026-05-07

using EDIVE.Input.Touch;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

namespace EDIVE.Input.Controls
{
    public enum CameraMovementMode
    {
        Planar,
        Flying
    }

    [RequireComponent(typeof(Camera))]
    public class PlayerCameraController : MonoBehaviour
    {
        [SerializeField]
        private CameraMovementMode _Mode = CameraMovementMode.Planar;
        
        [PropertySpace]
        [Required] 
        [SerializeField]
        private InputActionReference _Move;

        [Required] 
        [SerializeField]
        private InputActionReference _Look;
        
        [SerializeField] 
        private InputActionReference _Sprint;
        
        [ShowIf(nameof(_Mode), CameraMovementMode.Flying)]
        [SerializeField] 
        private InputActionReference _Vertical;

        [PropertySpace]
        [MinValue(0f)] 
        [SerializeField]
        private float _MoveSpeed = 3f;

        [MinValue(1f)] 
        [SerializeField]
        private float _SprintMultiplier = 3f;

        [Tooltip("Time to reach 99% of target position. 0 = instant.")]
        [Range(0f, 1f)] 
        [SerializeField]
        private float _PositionSmoothing;

        [PropertySpace]
        [MinValue(0f)]
        [SerializeField]
        private float _LookSensitivity = 120f;
        
        [SerializeField] 
        private float _MinPitch = -80f;
        
        [SerializeField] 
        private float _MaxPitch = 80f;
        
        [SerializeField] 
        private bool _InvertY;
        
        [Tooltip("Time to reach 99% of target rotation. 0 = instant.")]
        [Range(0f, 1f)] 
        [SerializeField]
        private float _RotationSmoothing;

        [PropertySpace]
        [SerializeField] 
        private bool _LockCursorOnRightMouse = true;
        
        [SerializeField] 
        private bool _UseGyro;
        
        [ShowIf(nameof(_UseGyro))]
        [SerializeField] 
        private float _GyroSensitivity = 1f;

        private float _yaw;
        private float _pitch;
        private float _smoothedYaw;
        private float _smoothedPitch;
        private Vector3 _targetPosition;
        private Vector3 _smoothedPosition;
        private bool _lookFromDelta;

        private void Awake()
        {
            var euler = transform.eulerAngles;
            _yaw = _smoothedYaw = euler.y;
            _pitch = _smoothedPitch = NormalizePitch(euler.x);
            _targetPosition = _smoothedPosition = transform.position;
        }

        private void OnEnable()
        {
            EnableAction(_Move);
            EnableAction(_Look);
            EnableAction(_Vertical);
            EnableAction(_Sprint);

            if (_Look != null)
                _Look.action.performed += OnLookPerformed;

            if (_UseGyro)
                TryEnableGyro(true);
        }

        private void OnDisable()
        {
            DisableAction(_Move);
            DisableAction(_Look);
            DisableAction(_Vertical);
            DisableAction(_Sprint);

            if (_Look != null)
                _Look.action.performed -= OnLookPerformed;

            TryEnableGyro(false);
        }

        private void Update()
        {
            HandleCursor();
            ApplyLook();
            ApplyMove();
            ApplySmoothing();
        }

        public void SetGyroEnabled(bool useGyro)
        {
            _UseGyro = useGyro;
            TryEnableGyro(useGyro);
        }

        public void Teleport(Vector3 position, Quaternion? rotation = null)
        {
            transform.position = position;
            _targetPosition = _smoothedPosition = position;

            if (rotation.HasValue)
            {
                var euler = rotation.Value.eulerAngles;
                _yaw = _smoothedYaw = euler.y;
                _pitch = _smoothedPitch = NormalizePitch(euler.x);
                transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
            }
        }

        private void HandleCursor()
        {
            if (!_LockCursorOnRightMouse || Mouse.current == null) return;

            if (Mouse.current.rightButton.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.Locked;
            else if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void ApplyLook()
        {
            if (_Look != null)
            {
                var look = _Look.action.ReadValue<Vector2>();
                // Mouse delta is per-frame; sticks are continuous values.
                var dtMult = _lookFromDelta ? 1f : Time.deltaTime;
                _yaw   += look.x * _LookSensitivity * dtMult;
                _pitch += look.y * _LookSensitivity * dtMult * (_InvertY ? 1f : -1f);
            }

            if (_UseGyro && Gyroscope.current != null && Gyroscope.current.enabled)
            {
                var rate = Gyroscope.current.angularVelocity.ReadValue() * (Mathf.Rad2Deg * _GyroSensitivity);
                _yaw   -= rate.y * Time.deltaTime;
                _pitch -= rate.x * Time.deltaTime;
            }

            _pitch = Mathf.Clamp(_pitch, _MinPitch, _MaxPitch);
        }

        private void ApplyMove()
        {
            if (_Move == null) return;

            var move = _Move.action.ReadValue<Vector2>();
            var vertical = _Vertical != null && _Mode == CameraMovementMode.Flying
                ? _Vertical.action.ReadValue<float>()
                : 0f;

            if (move.sqrMagnitude < 0.0001f && Mathf.Abs(vertical) < 0.0001f)
                return;

            var speed = _MoveSpeed * (IsSprintHeld() ? _SprintMultiplier : 1f);
            Vector3 dir;

            if (_Mode == CameraMovementMode.Flying)
            {
                // Camera-relative 6DOF (yaw + pitch), like the legacy desktop controller.
                var rot = Quaternion.Euler(_pitch, _yaw, 0f);
                dir = rot * new Vector3(move.x, vertical, move.y);
            }
            else
            {
                // Yaw-only XZ planar movement.
                var rot = Quaternion.Euler(0f, _yaw, 0f);
                dir = rot * new Vector3(move.x, 0f, move.y);
            }

            _targetPosition += dir * (speed * Time.deltaTime);
        }

        private void ApplySmoothing()
        {
            var posPct = SmoothingFactor(_PositionSmoothing);
            var rotPct = SmoothingFactor(_RotationSmoothing);

            _smoothedPosition = Vector3.Lerp(_smoothedPosition, _targetPosition, posPct);
            _smoothedYaw      = Mathf.LerpAngle(_smoothedYaw, _yaw, rotPct);
            _smoothedPitch    = Mathf.Lerp(_smoothedPitch, _pitch, rotPct);

            transform.position = _smoothedPosition;
            transform.eulerAngles = new Vector3(_smoothedPitch, _smoothedYaw, 0f);
        }

        private static float SmoothingFactor(float time)
        {
            if (time <= 0f) return 1f;
            // 99% of the way in `time` seconds, frame-rate independent.
            return 1f - Mathf.Exp(Mathf.Log(0.01f) / time * Time.deltaTime);
        }

        private bool IsSprintHeld()
        {
            if (_Sprint == null) return false;
            return _Sprint.action.IsPressed();
        }

        private void OnLookPerformed(InputAction.CallbackContext ctx)
        {
            // Mouse/pen deltas and LookPad-driven TouchControls are per-frame deltas; sticks are continuous.
            var device = ctx.control?.device;
            _lookFromDelta = device is Mouse || device is Pen || device is TouchControls;
        }

        private static void EnableAction(InputActionReference reference)
        {
            if (reference != null && reference.action != null)
                reference.action.Enable();
        }

        private static void DisableAction(InputActionReference reference)
        {
            if (reference != null && reference.action != null)
                reference.action.Disable();
        }

        private static void TryEnableGyro(bool enable)
        {
            var sensor = Gyroscope.current;
            if (sensor == null) return;
            if (enable && !sensor.enabled) InputSystem.EnableDevice(sensor);
            else if (!enable && sensor.enabled) InputSystem.DisableDevice(sensor);
        }

        private static float NormalizePitch(float pitch) => pitch > 180f ? pitch - 360f : pitch;
    }
}
