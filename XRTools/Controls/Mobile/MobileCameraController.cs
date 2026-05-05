// Author: František Holubec
// Created: 05.05.2026

using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

namespace EDIVE.XRTools.Controls.Mobile
{
    [RequireComponent(typeof(Camera))]
    public class MobileCameraController : MonoBehaviour
    {
        [Header("Joysticks")]
        [SerializeField] private AJoystickProvider _MoveJoystick;
        [SerializeField] private AJoystickProvider _LookJoystick;

        [Header("Movement")]
        [SerializeField] private float _MoveSpeed = 3f;

        [Header("Look")]
        [SerializeField] private float _LookSensitivity = 120f;
        [SerializeField] private float _MinPitch = -80f;
        [SerializeField] private float _MaxPitch = 80f;
        [SerializeField] private bool _InvertY;

        [Header("Gyro")]
        [SerializeField] private bool _UseGyro;
        [SerializeField] private float _GyroSensitivity = 1f;

        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            var euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = NormalizePitch(euler.x);
        }

        private void OnEnable()
        {
            if (_UseGyro) TryEnableGyro(true);
        }

        private void OnDisable()
        {
            TryEnableGyro(false);
        }

        public void SetGyroEnabled(bool useGyro)
        {
            _UseGyro = useGyro;
            TryEnableGyro(useGyro);
        }

        public void Teleport(Vector3 position, Quaternion? rotation = null)
        {
            transform.position = position;
            if (rotation.HasValue)
            {
                var euler = rotation.Value.eulerAngles;
                _yaw = euler.y;
                _pitch = NormalizePitch(euler.x);
            }
        }

        private void Update()
        {
            ApplyLook();
            ApplyMove();
            transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
        }

        private void ApplyLook()
        {
            if (_LookJoystick != null)
            {
                var look = _LookJoystick.Value;
                _yaw   += look.x * _LookSensitivity * Time.deltaTime;
                _pitch += look.y * _LookSensitivity * Time.deltaTime * (_InvertY ? 1f : -1f);
            }

            if (_UseGyro && Gyroscope.current != null && Gyroscope.current.enabled)
            {
                // Angular velocity in rad/s on the device's local axes; landscape phone:
                //   Y → yaw (turn left/right), X → pitch (tilt up/down).
                var rate = Gyroscope.current.angularVelocity.ReadValue() * (Mathf.Rad2Deg * _GyroSensitivity);
                _yaw   -= rate.y * Time.deltaTime;
                _pitch -= rate.x * Time.deltaTime;
            }

            _pitch = Mathf.Clamp(_pitch, _MinPitch, _MaxPitch);
        }

        private void ApplyMove()
        {
            if (_MoveJoystick == null) return;
            var move = _MoveJoystick.Value;
            if (move.sqrMagnitude < 0.0001f) return;

            var yawOnly = Quaternion.Euler(0f, _yaw, 0f);
            var dir = yawOnly * new Vector3(move.x, 0f, move.y);
            transform.position += dir * (_MoveSpeed * Time.deltaTime);
        }

        private void TryEnableGyro(bool enable)
        {
            var sensor = Gyroscope.current;
            if (sensor == null) return;
            if (enable && !sensor.enabled) InputSystem.EnableDevice(sensor);
            else if (!enable && sensor.enabled) InputSystem.DisableDevice(sensor);
        }

        private static float NormalizePitch(float pitch) => pitch > 180f ? pitch - 360f : pitch;
    }
}
