using System.Collections.Generic;
using System.Linq;
using EDIVE.Core.Services;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
        private bool _AutoAddKeyboardDisplay = true;

        [SerializeField]
        private bool _RepositionOutOfViewKeyboardOnOpen = true;

        [SerializeField]
        [Range(0, 1)]
        private float _FacingKeyboardThreshold = 0.15f;

        [SerializeField]
        private TransformScriptableVariable _CameraTransformVariable;

        [SerializeField]
        private List<InputActionAsset> _InputActions;

        public KeyboardController Keyboard => _Keyboard;
        private Transform CameraTransform =>
            _CameraTransformVariable != null && _CameraTransformVariable.Value != null
                ? _CameraTransformVariable.Value
                : Camera.main?.transform;

        private GameObject _lastFocusedObject;
        private readonly List<InputAction> _keyboardActions = new();
        private readonly List<InputAction> _disabledKeyboardActions = new();
        private bool _inputActionsBlocked;

        protected void Awake()
        {
            _Keyboard.gameObject.SetActive(false);
            CacheKeyboardActions();
        }

        private void Update()
        {
            var current = EventSystem.current?.currentSelectedGameObject;
            if (current != _lastFocusedObject)
            {
                if (TryGetInputField(current, out var inputField))
                    OnInputFieldFocused(inputField);
                else
                    OnInputFieldFocusLost();

            }

            _lastFocusedObject = current;
        }

        private static bool TryGetInputField(GameObject go, out AInputFieldWrapper inputField)
        {
            inputField = null;
            if (go == null)
                return false;

            if (go.TryGetComponent<TMP_InputField>(out var tmpInputField))
            {
                inputField = new TMPInputFieldWrapper(tmpInputField);
                return true;
            }

            if (go.TryGetComponent<InputField>(out var nativeInputField))
            {
                inputField = new NativeInputFieldWrapper(nativeInputField);
                return true;
            }

            return false;
        }

        private void OnInputFieldFocused(AInputFieldWrapper inputField)
        {
            if (_AutoAddKeyboardDisplay && !inputField.GameObject.TryGetComponent<KeyboardDisplay>(out _))
            {
                var keyboardDisplay = inputField.GameObject.AddComponent<KeyboardDisplay>();
                keyboardDisplay.ManualSelect();
            }

            BlockPhysicalKeyboard();
        }

        private void OnInputFieldFocusLost()
        {
            UnblockPhysicalKeyboard();
        }

        public KeyboardController ShowKeyboard(AInputFieldWrapper inputField, bool observeCharacterLimit = false)
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

        public void BlockPhysicalKeyboard()
        {
            if (_inputActionsBlocked || !InputSystem.devices.OfType<UnityEngine.InputSystem.Keyboard>().Any())
                return;
            
            foreach (var keyboardAction in _keyboardActions)
            {
                if (!keyboardAction.enabled)
                    continue;
                
                _disabledKeyboardActions.Add(keyboardAction);
                keyboardAction.Disable();
            }
            _inputActionsBlocked = true;
        }

        public void UnblockPhysicalKeyboard()
        {
            if (!_inputActionsBlocked) 
                return;

            foreach (var keyboardAction in _disabledKeyboardActions)
                keyboardAction.Enable();

            _inputActionsBlocked = false;
        }

        private void CacheKeyboardActions()
        {
            foreach (var inputAction in _InputActions)
            foreach (var map in inputAction.actionMaps)
            foreach (var action in map.actions)
            {
                var hasKeyboard = false;
                foreach (var binding in action.bindings)
                {
                    if (binding.effectivePath != null && binding.effectivePath.Contains("<Keyboard>"))
                    {
                        hasKeyboard = true;
                        break;
                    }
                }
                if (hasKeyboard)
                    _keyboardActions.Add(action);
            }
        }
    }
}