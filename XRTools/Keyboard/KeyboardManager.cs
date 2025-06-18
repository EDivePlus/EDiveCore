using EDIVE.Core.Services;
using EDIVE.DataStructures.ScriptableVariables.Variables;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace EDIVE.XRTools.Keyboard
{
    public class KeyboardManager : AServiceBehaviour<KeyboardManager>
    {
        [SerializeField]
        private KeyboardController _Keyboard;

        [SerializeField]
        private Vector3 _KeyboardOffset;

        [SerializeField]
        private bool _RepositionOutOfViewKeyboardOnOpen = true;

        [SerializeField]
        [Range(0, 1)]
        private float _FacingKeyboardThreshold = 0.15f;

        [SerializeField]
        private TransformScriptableVariable _CameraTransformVariable;

        public KeyboardController Keyboard => _Keyboard;
        private Transform CameraTransform => _CameraTransformVariable.Value;

        protected override void Awake()
        {
            base.Awake();
            _Keyboard.gameObject.SetActive(false);
        }

        public KeyboardController ShowKeyboard(TMP_InputField inputField, bool observeCharacterLimit = false)
        {
            if (_Keyboard == null)
                return null;

            var shouldPositionKeyboard = !_Keyboard.IsOpen || (_RepositionOutOfViewKeyboardOnOpen && IsKeyboardOutOfView());
            _Keyboard.Open(inputField, observeCharacterLimit);

            if (shouldPositionKeyboard)
                PositionKeyboard(CameraTransform);


            return Keyboard;
        }

        public KeyboardController ShowKeyboard(string text)
        {
            if (_Keyboard == null)
                return null;

            var shouldPositionKeyboard = !_Keyboard.IsOpen || (_RepositionOutOfViewKeyboardOnOpen && IsKeyboardOutOfView());
            _Keyboard.Open(text);

            if (shouldPositionKeyboard)
                PositionKeyboard(CameraTransform);

            return Keyboard;
        }

        public KeyboardController ShowKeyboard(bool clearKeyboardText = false)
        {
            if (_Keyboard == null)
                return null;

            ShowKeyboard(clearKeyboardText ? string.Empty : _Keyboard.Text);

            return Keyboard;
        }

        public virtual void HideKeyboard()
        {
            if (_Keyboard == null)
                return;

            _Keyboard.Close();
        }

        public void RepositionKeyboardIfOutOfView()
        {
            if (IsKeyboardOutOfView())
            {
                if (_Keyboard.IsOpen)
                    PositionKeyboard(CameraTransform);
            }
        }

        private void PositionKeyboard(Transform target)
        {
            if (target == null)
                return;

            var position = target.position +
                target.right * _KeyboardOffset.x +
                target.forward * _KeyboardOffset.z +
                Vector3.up * _KeyboardOffset.y;
            _Keyboard.transform.position = position;
            _Keyboard.transform.localScale = Vector3.one;
            FaceKeyboardAtTarget(CameraTransform);
        }

        private void FaceKeyboardAtTarget(Transform target)
        {
            var forward = (_Keyboard.transform.position - target.position).normalized;
            BurstMathUtility.OrthogonalLookRotation(forward, Vector3.up, out var newTarget);
            _Keyboard.transform.rotation = newTarget;
        }

        private bool IsKeyboardOutOfView()
        {
            if (CameraTransform == null || _Keyboard == null)
            {
                Debug.LogWarning("Camera or keyboard reference is null. Unable to determine if keyboard is out of view.", this);
                return false;
            }

            var dotProduct = Vector3.Dot(CameraTransform.forward, (_Keyboard.transform.position - CameraTransform.position).normalized);
            return dotProduct < _FacingKeyboardThreshold;
        }
    }
}