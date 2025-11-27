using EDIVE.Core;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.XRTools.Keyboard
{
    public class KeyboardDisplay : MonoBehaviour
    {
        [SerializeReference]
        private AInputFieldWrapper _InputField;

        [SerializeField]
        private bool _ManualKeyboard;

        [EnableIf(nameof(_ManualKeyboard))]
        [SerializeField]
        private KeyboardController _Keyboard;

        [SerializeField]
        private bool _OpenKeyboardOnFocus = true;

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
        
        public KeyboardController Keyboard => _Keyboard;

        private bool _isActivelyObservingKeyboard;
        private KeyboardController _activeKeyboard;

        private int _lastCaretPosition;

        private void Awake()
        {
            if (!XRUtils.XREnabled)
                return;

            if (_InputField == null || !_InputField.IsValid())
            {
                if (TryGetComponent<TMP_InputField>(out var tmpInputField))
                {
                    _InputField = new TMPInputFieldWrapper(tmpInputField);
                }
                else if (TryGetComponent<InputField>(out var nativeInputField))
                { 
                    _InputField = new NativeInputFieldWrapper(nativeInputField);
                }
                else
                {
                    Debug.LogError("No supported InputField component found on the GameObject.");
                    return;
                }
            }

            _activeKeyboard = Keyboard;
            _InputField.PrepareForKeyboard(); 

            if (_AlwaysObserveKeyboard && _activeKeyboard != null)
                StartObservingKeyboard(_activeKeyboard);
        }

        private void OnEnable()
        {
            if (!XRUtils.XREnabled)
                return;

            if (_InputField != null)
            {
                _InputField.GainedFocus += OnInputFieldGainedFocus;
                _InputField.TextSelectionChanged += OnTextSelectionChanged;
                _lastCaretPosition = _InputField.CaretPosition;
            }
        }

        private void Start()
        {
            if (!XRUtils.XREnabled)
                return;

            if (_activeKeyboard == null || !_ManualKeyboard)
                _activeKeyboard = AppCore.Services.Get<KeyboardManager>().Keyboard;

            var observeOnStart = _AlwaysObserveKeyboard && _activeKeyboard != null && !_isActivelyObservingKeyboard;
            if (observeOnStart)
                StartObservingKeyboard(_activeKeyboard);
        }

        private void Update()
        {
            if (!XRUtils.XREnabled)
                return;

            if (_InputField != null && _isActivelyObservingKeyboard && _activeKeyboard != null)
            {
                var currentCaret = _InputField.CaretPosition;
                if (currentCaret != _lastCaretPosition)
                {
                    _lastCaretPosition = currentCaret;
                    _activeKeyboard.CaretPosition = currentCaret;
                }
            }
        }

        private void OnDisable()
        {
            if (!XRUtils.XREnabled)
                return;

            if (_InputField != null)
            {
                _InputField.GainedFocus -= OnInputFieldGainedFocus;
                _InputField.TextSelectionChanged -= OnTextSelectionChanged;
            }
            
            var isObservingKeyboard = _activeKeyboard != null && _activeKeyboard.gameObject.activeInHierarchy && _isActivelyObservingKeyboard;
            if (_HideKeyboardOnDisable && isObservingKeyboard && _activeKeyboard.IsOpen)
                _activeKeyboard.Close();
        }

        private void OnDestroy()
        {
            if (!XRUtils.XREnabled)
                return;

            StopObservingKeyboard(_activeKeyboard);
        }

        private void OnTextSelectionChanged(string selectedText, int startIndex, int endIndex)
        {
            _activeKeyboard.SelectStartIndex = startIndex;
            _activeKeyboard.SelectEndIndex = endIndex;
        }

        public void ManualSelect()
        {
            OnInputFieldGainedFocus();
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

        private void OnInputFieldGainedFocus()
        {
            if (_isActivelyObservingKeyboard && !_AlwaysObserveKeyboard)
            {
                if (!_ManualKeyboard || Keyboard == null)
                    AppCore.Services.Get<KeyboardManager>().RepositionKeyboardIfOutOfView();

                _InputField.CaretPosition = _activeKeyboard.CaretPosition;
                return;
            }

            if (_ClearTextOnOpen)
                _InputField.Text = string.Empty;

            if (_OpenKeyboardOnFocus)
            {
                if (_ManualKeyboard && Keyboard != null)
                {
                    _activeKeyboard.Open(_InputField, _MonitorCharacterLimit);
                }
                else
                {
                    var provider = GetComponentInParent<KeyboardProvider>();
                    if (provider != null && provider.Keyboard != null)
                    {
                        provider.Keyboard.Open(_InputField, _MonitorCharacterLimit);
                        _activeKeyboard = provider.Keyboard;
                    }
                    else
                    {
                        _activeKeyboard = AppCore.Services.Get<KeyboardManager>().ShowKeyboard(_InputField, _MonitorCharacterLimit);
                    }
                }
            }

            _InputField.CaretPosition = _activeKeyboard.CaretPosition;

            StartObservingKeyboard(_activeKeyboard);
        }

        private void OnTextSubmit(string text)
        {
            UpdateText(text);
            if (_ClearTextOnSubmit)
            {
                _InputField.Text = string.Empty;
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

            if (_MonitorCharacterLimit && updatedText.Length >= _InputField.CharacterLimit)
                updatedText = updatedText[.._InputField.CharacterLimit];

            _InputField.Text = updatedText;
            _InputField.CaretPosition = _activeKeyboard.CaretPosition;
            _InputField.SelectionAnchorPosition = _activeKeyboard.SelectStartIndex;
            _InputField.SelectionFocusPosition = _activeKeyboard.SelectEndIndex;
        }

        private void KeyboardOpening()
        {
            if (!_InputField.IsFocused && !_AlwaysObserveKeyboard)
                StopObservingKeyboard(_activeKeyboard);
        }

        private void KeyboardClosing()
        {
            if (!_AlwaysObserveKeyboard)
                StopObservingKeyboard(_activeKeyboard);
        }

        private void KeyboardFocusChanged()
        {
            if (!_InputField.IsFocused && !_AlwaysObserveKeyboard)
                StopObservingKeyboard(_activeKeyboard);
        }
    }
}
