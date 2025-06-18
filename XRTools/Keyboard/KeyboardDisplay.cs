using EDIVE.Core;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.XRTools.Keyboard
{
    public class KeyboardDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField _InputField;

        [SerializeField]
        private bool _UseSceneKeyboard;

        [EnableIf(nameof(_UseSceneKeyboard))]
        [SerializeField]
        private KeyboardController _Keyboard;

        [SerializeField]
        private bool _AlwaysObserveKeyboard;

        [SerializeField]
        private bool _HideKeyboardOnDisable = true;

        [SerializeField]
        private bool _UpdateOnKeyPress = true;

        [SerializeField]
        private bool _MonitorCharacterLimit;

        [SerializeField]
        private bool _ClearTextOnSubmit;

        [SerializeField]
        private bool _ClearTextOnOpen;


        public TMP_InputField InputField
        {
            get => _InputField;
            set
            {
                if (_InputField != null)
                    _InputField.onSelect.RemoveListener(OnInputFieldGainedFocus);

                _InputField = value;

                if (_InputField != null)
                {
                    _InputField.resetOnDeActivation = false;
                    _InputField.onSelect.AddListener(OnInputFieldGainedFocus);
                }
            }
        }

        public KeyboardController Keyboard
        {
            get => _Keyboard;
            set => SetKeyboard(value);
        }

        private bool _isActivelyObservingKeyboard;
        private KeyboardController _activeKeyboard;

        private void Awake()
        {
            _activeKeyboard = Keyboard;
            if (InputField != null)
            {
                InputField.resetOnDeActivation = false;
                InputField.shouldHideSoftKeyboard = true;
            }

            if (_AlwaysObserveKeyboard && _activeKeyboard != null)
                StartObservingKeyboard(_activeKeyboard);
        }

        private void OnEnable()
        {
            if (InputField != null)
                InputField.onSelect.AddListener(OnInputFieldGainedFocus);
        }

        private void OnDisable()
        {
            if (InputField != null)
                InputField.onSelect.RemoveListener(OnInputFieldGainedFocus);

            var isObservingKeyboard = _activeKeyboard != null && _activeKeyboard.gameObject.activeInHierarchy && _isActivelyObservingKeyboard;
            if (_HideKeyboardOnDisable && isObservingKeyboard && _activeKeyboard.IsOpen)
                _activeKeyboard.Close();
        }

        private void OnDestroy()
        {
            StopObservingKeyboard(_activeKeyboard);
        }

        private void Start()
        {
            if (_activeKeyboard == null || !_UseSceneKeyboard)
                _activeKeyboard = AppCore.Services.Get<KeyboardManager>().Keyboard;

            var observeOnStart = _AlwaysObserveKeyboard && _activeKeyboard != null && !_isActivelyObservingKeyboard;
            if (observeOnStart)
                StartObservingKeyboard(_activeKeyboard);
        }

        private void SetKeyboard(KeyboardController updateKeyboard, bool observeKeyboard = true)
        {
            if (ReferenceEquals(updateKeyboard, _Keyboard))
                return;

            StopObservingKeyboard(_activeKeyboard);
            _Keyboard = updateKeyboard;
            _activeKeyboard = _Keyboard;

            if (_activeKeyboard != null && (observeKeyboard || _AlwaysObserveKeyboard))
                StartObservingKeyboard(_activeKeyboard);
        }

        private void StartObservingKeyboard(KeyboardController activeKeyboard)
        {
            if (activeKeyboard == null || _isActivelyObservingKeyboard)
                return;

            activeKeyboard.TextUpdated.AddListener(OnTextUpdate);
            activeKeyboard.TextSubmitted.AddListener(OnTextSubmit);
            activeKeyboard.Closed.AddListener(KeyboardClosing);
            activeKeyboard.Opened.AddListener(KeyboardOpening);
            activeKeyboard.FocusChanged.AddListener(KeyboardFocusChanged);

            _isActivelyObservingKeyboard = true;
        }

        private void StopObservingKeyboard(KeyboardController activeKeyboard)
        {
            if (activeKeyboard == null)
                return;

            activeKeyboard.TextUpdated.RemoveListener(OnTextUpdate);
            activeKeyboard.TextSubmitted.RemoveListener(OnTextSubmit);
            activeKeyboard.Closed.RemoveListener(KeyboardClosing);
            activeKeyboard.Opened.RemoveListener(KeyboardOpening);
            activeKeyboard.FocusChanged.RemoveListener(KeyboardFocusChanged);

            _isActivelyObservingKeyboard = false;
        }

        private void OnInputFieldGainedFocus(string text)
        {
            // If this display is already observing keyboard, sync, attempt to reposition, and early out
            // Displays that are always observing keyboards call open to ensure they sync with the keyboard
            if (_isActivelyObservingKeyboard && !_AlwaysObserveKeyboard)
            {
                if (!_UseSceneKeyboard || Keyboard == null)
                    AppCore.Services.Get<KeyboardManager>().RepositionKeyboardIfOutOfView();

                if (InputField.stringPosition != _activeKeyboard.CaretPosition)
                    InputField.stringPosition = _activeKeyboard.CaretPosition;

                return;
            }

            if (_ClearTextOnOpen)
                InputField.text = string.Empty;

            // If not using a scene keyboard, use global keyboard.
            if (!_UseSceneKeyboard || Keyboard == null)
            {
                AppCore.Services.Get<KeyboardManager>().ShowKeyboard(InputField, _MonitorCharacterLimit);
            }
            else
            {
                _activeKeyboard.Open(InputField, _MonitorCharacterLimit);
            }

            if (_InputField.stringPosition != _activeKeyboard.CaretPosition)
                _InputField.stringPosition = _activeKeyboard.CaretPosition;

            StartObservingKeyboard(_activeKeyboard);
        }

        private void OnTextSubmit(string text)
        {
            UpdateText(text);
            if (_ClearTextOnSubmit)
            {
                InputField.text = string.Empty;
            }
        }

        private void OnTextUpdate(string text)
        {
            if (!_UpdateOnKeyPress)
                return;

            UpdateText(text);
        }

        private void UpdateText(string text)
        {
            var updatedText = text;

            if (_MonitorCharacterLimit && updatedText.Length >= InputField.characterLimit)
                updatedText = updatedText.Substring(0, InputField.characterLimit);

            InputField.text = updatedText;
            if (InputField.stringPosition != _activeKeyboard.CaretPosition)
                InputField.stringPosition = _activeKeyboard.CaretPosition;
        }

        private void KeyboardOpening()
        {
            if (!InputField.isFocused && !_AlwaysObserveKeyboard)
                StopObservingKeyboard(_activeKeyboard);
        }

        private void KeyboardClosing()
        {
            if (!_AlwaysObserveKeyboard)
                StopObservingKeyboard(_activeKeyboard);
        }

        private void KeyboardFocusChanged()
        {
            if (!InputField.isFocused && !_AlwaysObserveKeyboard)
                StopObservingKeyboard(_activeKeyboard);
        }
    }
}
