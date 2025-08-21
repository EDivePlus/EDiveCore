// Created by Vojtech Bruza, strongly inspired by Unity Template SimpleCameraController

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools.Controls.Legacy
{
    [RequireComponent(typeof(Camera))]
    public class DesktopCameraController : MonoBehaviour
    {
        // TODO not working - somehow read the input
        //private Edive_DesktopControls controls;

        private Camera cam;

        public int currentPositionIndex = 0;
        public List<Transform> defaultPositions = new List<Transform>();

        private class CameraState
        {
            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            public void SetFromTransform(Transform t)
            {
                pitch = t.eulerAngles.x;
                yaw = t.eulerAngles.y;
                roll = t.eulerAngles.z;
                x = t.position.x;
                y = t.position.y;
                z = t.position.z;
            }

            public void Translate(Vector3 translation)
            {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;

                x += rotatedTranslation.x;
                y += rotatedTranslation.y;
                z += rotatedTranslation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);

                x = Mathf.Lerp(x, target.x, positionLerpPct);
                y = Mathf.Lerp(y, target.y, positionLerpPct);
                z = Mathf.Lerp(z, target.z, positionLerpPct);
            }

            public void UpdateTransform(Transform t)
            {
                t.eulerAngles = new Vector3(pitch, yaw, roll);
                t.position = new Vector3(x, y, z);
            }
        }

        private CameraState m_TargetCameraState = new CameraState();
        private CameraState m_InterpolatingCameraState = new CameraState();

        [Tooltip("If true, then the camera cannot be moved.")]
        public bool disableMovement = false;

        [Header("Movement Settings")]
        public float cameraSpeed = 1;

        [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float boost = 1.5f;

        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float positionLerpTime = 0.2f;

        [Header("Rotation Settings")]
        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));
        [Tooltip("Mouse rotation sensitivity")]
        public float mouseSensitivity = 0.03f;

        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float rotationLerpTime = 0.01f;

        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool invertY = false;

        private void OnEnable()
        {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);

            //controls.Controls.DesktopCamera.Move.performed += MoveCamera;
            //controls.Controls.DesktopCamera.LeftMouse.performed += LeftMousePress;

            if (UnityEngine.InputSystem.Keyboard.current != null)
                UnityEngine.InputSystem.Keyboard.current.onTextInput += OnKeyboardInput;
        }

        private void OnKeyboardInput(char c)
        {
            // change camera position?
            bool changed = false;
            // number on num pad
            int pressedNumber = c - '0';
            if (pressedNumber >= 0 && pressedNumber < defaultPositions.Count)
            {
                currentPositionIndex = pressedNumber;
                changed = true;
            }
            // plus cycle forward
            else if (c == '+')
            {
                currentPositionIndex = currentPositionIndex >= defaultPositions.Count ? 0 : currentPositionIndex + 1;
                changed = true;
            }
            // plus cycle back
            else if (c == '-')
            {
                currentPositionIndex = currentPositionIndex < 0 ? defaultPositions.Count - 1 : currentPositionIndex - 1;
                changed = true;
            }
            if (changed)
            {
                var pos = defaultPositions[currentPositionIndex];
                m_TargetCameraState.SetFromTransform(pos);
                m_InterpolatingCameraState.SetFromTransform(pos);
            }
        }

        private void OnDisable()
        {
            //controls.Controls.DesktopCamera.Move.performed -= MoveCamera;
            //controls.Controls.DesktopCamera.LeftMouse.performed -= LeftMousePress;

            UnityEngine.InputSystem.Keyboard.current.onTextInput -= OnKeyboardInput;
        }

        private void Awake()
        {
            // TODO check simple camera controller from unity?
            // Mouse.current.leftButton.
            cam = GetComponent<Camera>();
            // TODO not working
            //controls = (Edive_DesktopControls)(Edive_DesktopControls.Instance);
        }

        private void LeftMousePress(bool pressed)//InputAction.CallbackContext context)
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue()); // TODO how to get mouse position

            if (Physics.Raycast(ray, out hit))
            {
                Transform objectHit = hit.transform;

                var interactable = objectHit.gameObject.GetComponentInParent<XRSimpleInteractable>();

                if (pressed)
                {
                    TryInvokeSelectEnterEvents(interactable);
                }
                else
                {
                    TryInvokeSelectExitEvents(interactable);
                }
            }
        }

        private void TryInvokeSelectEnterEvents(XRSimpleInteractable xrSimpleInteractable)
        {
            if (xrSimpleInteractable)
            {
                xrSimpleInteractable.firstHoverEntered.Invoke(new HoverEnterEventArgs());
                xrSimpleInteractable.hoverEntered.Invoke(new HoverEnterEventArgs());
                xrSimpleInteractable.selectEntered.Invoke(new SelectEnterEventArgs());
            }
        }

        private void TryInvokeSelectExitEvents(XRSimpleInteractable xrSimpleInteractable)
        {
            if (xrSimpleInteractable)
            {
                xrSimpleInteractable.selectExited.Invoke(new SelectExitEventArgs());
                xrSimpleInteractable.hoverExited.Invoke(new HoverExitEventArgs());
                xrSimpleInteractable.lastHoverExited.Invoke(new HoverExitEventArgs());
            }
        }

        //private void MoveCamera(InputAction.CallbackContext context)
        //{
        //    var value = context.ReadValue<Vector2>();
        //    // TODO use this instead of update?
        //}

        private Vector3 GetInputTranslationDirection()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return Vector3.zero;
            Vector3 direction = new Vector3();
            if (UnityEngine.InputSystem.Keyboard.current[Key.W].isPressed)
            {
                direction += Vector3.forward;
            }
            if (UnityEngine.InputSystem.Keyboard.current[Key.S].isPressed)
            {
                direction += Vector3.back;
            }
            if (UnityEngine.InputSystem.Keyboard.current[Key.A].isPressed)
            {
                direction += Vector3.left;
            }
            if (UnityEngine.InputSystem.Keyboard.current[Key.D].isPressed)
            {
                direction += Vector3.right;
            }
            if (UnityEngine.InputSystem.Keyboard.current[Key.Q].isPressed)
            {
                direction += Vector3.down;
            }
            if (UnityEngine.InputSystem.Keyboard.current[Key.E].isPressed)
            {
                direction += Vector3.up;
            }
            return direction;
        }

        private void Update()
        {
#if UNITY_EDITOR || !UNITY_SERVER
            Vector3 translation = Vector3.zero;

            if (UnityEngine.InputSystem.Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
            // Hide and lock cursor when right mouse button pressed
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                LeftMousePress(true);
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                LeftMousePress(false);
            }

            // Hide and lock cursor when right mouse button pressed
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // Unlock and show cursor when right mouse button released
            if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            if (disableMovement)
            {
                return;
            }

            // Rotation
            if (Mouse.current.rightButton.isPressed)
            {
                float x = Mouse.current.delta.x.ReadValue() * mouseSensitivity;
                float y = Mouse.current.delta.y.ReadValue() * mouseSensitivity;
                var mouseMovement = new Vector2(x, y * (invertY ? 1 : -1));

                var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
                m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
            }

            // Translation
            translation = GetInputTranslationDirection() * cameraSpeed * Time.deltaTime;

            // Speed up movement when shift key held
            if (UnityEngine.InputSystem.Keyboard.current[Key.LeftShift].isPressed)
            {
                translation *= 10.0f;
            }

            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            boost += Mouse.current.scroll.ReadDefaultValue().y * 0.2f;
            translation *= Mathf.Pow(2.0f, boost);

            m_TargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

            m_InterpolatingCameraState.UpdateTransform(transform);
#endif
        }

        public void Teleport(Vector3 position, Quaternion? rotation = null)
        {
            transform.position = position;
            if (rotation.HasValue)
                transform.rotation = rotation.Value;
            
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
        }
    }
}
