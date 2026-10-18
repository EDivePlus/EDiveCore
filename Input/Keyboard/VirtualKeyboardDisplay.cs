using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Input.Keyboard.InputFieldWrappers;
using EDIVE.View.ViewTree;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Input.Keyboard
{
    public class VirtualKeyboardDisplay : MonoBehaviour
    {
        [SerializeReference]
        private AInputFieldWrapper _InputField;

        [SerializeField]
        private TMP_Text _Label;

        [SerializeField]
        private bool _ManualKeyboard;

        [EnableIf(nameof(_ManualKeyboard))]
        [SerializeField]
        private VirtualKeyboardController _Keyboard;

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

        public VirtualKeyboardController Keyboard => _Keyboard;

        private bool _isActivelyObservingKeyboard;
        private VirtualKeyboardController _activeKeyboard;

        private int _lastCaretPosition;
        private bool _isAvailable;
        private bool _initialized;
        private IDisposable _serviceRegistration;
        private AViewNode _viewNode;

        private void Awake()
        {
            _serviceRegistration = AppCore.Services.WhenRegistered<VirtualKeyboardManager>(keyboardManager =>
            {
                if (!keyboardManager.CheckAvailable())
                    return;

                _isAvailable = true;
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

                // The manager can register after this field got enabled
                if (isActiveAndEnabled)
                    Initialize();
            });
        }

        private void Initialize()
        {
            if (!_isAvailable || _initialized)
                return;

            _initialized = true;
            if (_InputField != null)
            {
                _InputField.GainedFocus += OnInputFieldGainedFocus;
                _InputField.TextSelectionChanged += OnTextSelectionChanged;
                _lastCaretPosition = _InputField.CaretPosition;
                if (_AlwaysObserveKeyboard)
                    _InputField.ValueChanged += OnFieldValueChanged;
            }

            _viewNode = this.FindViewNode();
            if (_viewNode != null)
                _viewNode.StateChanged += OnViewStateChanged;

            if ((_activeKeyboard == null || !_ManualKeyboard) && AppCore.Services.TryGet<VirtualKeyboardManager>(out var keyboardManager))
                _activeKeyboard = keyboardManager.Keyboard;

            var observeOnStart = _AlwaysObserveKeyboard && _activeKeyboard != null && !_isActivelyObservingKeyboard;
            if (observeOnStart)
                StartObservingKeyboard(_activeKeyboard);
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            if (!_initialized)
                return;

            _initialized = false;
            if (_InputField != null)
            {
                _InputField.GainedFocus -= OnInputFieldGainedFocus;
                _InputField.TextSelectionChanged -= OnTextSelectionChanged;
                _InputField.ValueChanged -= OnFieldValueChanged;
            }

            if (_viewNode != null)
                _viewNode.StateChanged -= OnViewStateChanged;

            var isObservingKeyboard = _activeKeyboard != null && _activeKeyboard.gameObject.activeInHierarchy && _isActivelyObservingKeyboard;
            if (_HideKeyboardOnDisable && isObservingKeyboard && _activeKeyboard.IsOpen)
                _activeKeyboard.Close();
        }

        private void OnDestroy()
        {
            _serviceRegistration?.Dispose();
            _serviceRegistration = null;
            StopObservingKeyboard(_activeKeyboard);
        }

        private void Update()
        {
            if (!_isAvailable)
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

        private void OnTextSelectionChanged(string selectedText, int startIndex, int endIndex)
        {
            if (_activeKeyboard == null)
                return;

            _activeKeyboard.SelectStartIndex = startIndex;
            _activeKeyboard.SelectEndIndex = endIndex;
        }

        public void ManualSelect()
        {
            OnInputFieldGainedFocus();
        }

        private void SetKeyboard(VirtualKeyboardController updateKeyboard, bool observeKeyboard = true)
        {
            if (ReferenceEquals(updateKeyboard, _Keyboard))
                return;

            StopObservingKeyboard(_activeKeyboard);
            _Keyboard = updateKeyboard;
            _activeKeyboard = _Keyboard;

            if (_activeKeyboard != null && (observeKeyboard || _AlwaysObserveKeyboard))
                StartObservingKeyboard(_activeKeyboard);
        }

        private void StartObservingKeyboard(VirtualKeyboardController activeKeyboard)
        {
            if (activeKeyboard == null || _isActivelyObservingKeyboard)
                return;

            activeKeyboard.TextUpdated += OnTextUpdate;
            activeKeyboard.TextSubmitted += OnTextSubmit;
            activeKeyboard.Closed += KeyboardClosing;
            activeKeyboard.Opened += KeyboardOpening;
            activeKeyboard.FocusChanged += KeyboardFocusChanged;

            _isActivelyObservingKeyboard = true;

            // Text set before this display woke up, e.g. a prefilled field on the first open
            if (_AlwaysObserveKeyboard && _InputField != null && _InputField.IsValid())
            {
                MirrorPassword();
                UpdateText(activeKeyboard.Text);
            }
        }

        private void StopObservingKeyboard(VirtualKeyboardController activeKeyboard)
        {
            if (activeKeyboard == null)
                return;

            activeKeyboard.TextUpdated -= OnTextUpdate;
            activeKeyboard.TextSubmitted -= OnTextSubmit;
            activeKeyboard.Closed -= KeyboardClosing;
            activeKeyboard.Opened -= KeyboardOpening;
            activeKeyboard.FocusChanged -= KeyboardFocusChanged;

            _isActivelyObservingKeyboard = false;
        }

        private void OnInputFieldGainedFocus()
        {
            if (!_AlwaysObserveKeyboard && !_InputField.IsEditable)
                return;

            if (_isActivelyObservingKeyboard && !_AlwaysObserveKeyboard)
            {
                if ((!_ManualKeyboard || Keyboard == null) && AppCore.Services.TryGet<VirtualKeyboardManager>(out var keyboardManager))
                    keyboardManager.RepositionKeyboardIfOutOfView();

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
                    var provider = this.FindInViewTree<VirtualKeyboardProvider>() ?? GetComponentInParent<VirtualKeyboardProvider>();
                    if (provider != null && provider.Keyboard != null)
                    {
                        provider.Keyboard.Open(_InputField, _MonitorCharacterLimit);
                        _activeKeyboard = provider.Keyboard;
                    }
                    else if (AppCore.Services.TryGet<VirtualKeyboardManager>(out var keyboardManager))
                    {
                        _activeKeyboard = keyboardManager.ShowKeyboard(_InputField, _MonitorCharacterLimit);
                    }
                }
            }

            if (_activeKeyboard == null)
                return;

            if (_OpenKeyboardOnFocus && _Label != null)
                _activeKeyboard.Label = _Label.text;

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

            if (_MonitorCharacterLimit && _InputField.CharacterLimit > 0 && updatedText.Length >= _InputField.CharacterLimit)
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
            if (_AlwaysObserveKeyboard)
                MirrorPassword();
            else if (!_InputField.IsFocused)
                StopObservingKeyboard(_activeKeyboard);
        }

        // Physical typing into an always observing field, e.g. the keyboard's own preview
        private void OnFieldValueChanged(string text)
        {
            if (_isActivelyObservingKeyboard && _activeKeyboard != null && text != _activeKeyboard.Text)
                _activeKeyboard.SetText(text);
        }

        private void MirrorPassword()
        {
            var source = _activeKeyboard != null ? _activeKeyboard.CurrentInputField : null;
            _InputField.IsPassword = source != null && source.IsValid() && source.IsPassword;
        }

        // The field's window got hidden or covered, e.g. by a dialog
        private void OnViewStateChanged(ViewState from, ViewState to)
        {
            if (_AlwaysObserveKeyboard || _viewNode.FocusedInTree || _activeKeyboard == null || !_activeKeyboard.IsOpen)
                return;

            var current = _activeKeyboard.CurrentInputField;
            if (current != null && current.IsValid() && current.GameObject == _InputField.GameObject)
                _activeKeyboard.Close(false);
        }
    }
}
