using System;
using System.Collections.Generic;
using EDIVE.Input.Keyboard.InputFieldWrappers;
using EDIVE.StateHandling.MultiStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Input.Keyboard
{
    public class VirtualKeyboardController : MonoBehaviour
    {
        [SerializeField]
        private bool _SubmitOnEnter = true;

        [SerializeField]
        private bool _CloseOnSubmit = true;

        [SerializeField]
        [ValidateMultiState(typeof(KeyboardLayout))]
        private AMultiState _LayoutState;

        public AInputFieldWrapper CurrentInputField
        {
            get => _currentInputField;
            set
            {
                if (_currentInputField == value)
                    return;

                StopObservingInputField(_currentInputField);
                _currentInputField = value;
                StartObservingInputField(_currentInputField);

                FocusChanged?.Invoke();
            }
        }

        public string Text
        {
            get => _text;
            private set
            {
                if (_text == value)
                    return;

                _text = value;
                CaretPosition = Math.Clamp(CaretPosition, 0, _text.Length);
                TextUpdated?.Invoke(_text);
            }
        }

        public int CaretPosition
        {
            get => _caretPosition;
            set
            {
                _caretPosition = value;
                SelectStartIndex = SelectEndIndex = _caretPosition;
            }
        }
        public int SelectStartIndex { get; set; }
        public int SelectEndIndex { get; set; }

        public ShiftState ShiftState { get; private set; }
        public KeyboardLayout CurrentLayout { get; private set; }
        public bool IsShifted => ShiftState != ShiftState.None;
        public bool IsOpen => _isOpen && isActiveAndEnabled;

        public event Action Opened;
        public event Action Closed;
        public event Action<VirtualKeyboardKey> KeyPressed;
        public event Action<KeyboardLayout> LayoutChanged;
        public event Action<ShiftState> ShiftChanged;
        public event Action<string> TextUpdated;
        public event Action<string> TextSubmitted;
        public event Action FocusChanged;
        public event Action CharacterLimitReached;

        private AInputFieldWrapper _currentInputField;
        private List<VirtualKeyboardKey> _keys;
        private string _text = string.Empty;

        private bool _isOpen;
        private int _characterLimit;
        private bool _monitorCharacterLimit;
        private int _caretPosition;

        private void Awake()
        {
            _keys = new List<VirtualKeyboardKey>();
            GetComponentsInChildren(true, _keys);
            _keys.ForEach(key => key.Initialize(this));
            SetLayout(KeyboardLayout.Characters);
        }

        private void OnDisable()
        {
            _isOpen = false;
        }

        public void RaiseKeyPressed(VirtualKeyboardKey key) => KeyPressed?.Invoke(key);

        public virtual void InsertText(string newText)
        {
            var selectionStart = Mathf.Min(SelectStartIndex, SelectEndIndex);
            var selectionEnd = Mathf.Max(SelectStartIndex, SelectEndIndex);
            var selectionLength = selectionEnd - selectionStart;

            if (selectionLength > 0)
                CaretPosition = Mathf.Clamp(selectionStart, 0, Text.Length);

            var updatedText = Text.Remove(selectionStart, selectionLength);
            updatedText = updatedText.Insert(CaretPosition, newText);

            var isUpdatedTextWithinLimits = !_monitorCharacterLimit || updatedText.Length <= _characterLimit;
            if (isUpdatedTextWithinLimits)
            {
                CaretPosition += newText.Length;
                Text = updatedText;
            }
            else
            {
                CharacterLimitReached?.Invoke();
            }

            if (ShiftState == ShiftState.Shift)
                Shift(ShiftState.None);
        }

        public void SetLayout(KeyboardLayout layout)
        {
            CurrentLayout = layout;
            if (_LayoutState)
                _LayoutState.SetState(layout);
            LayoutChanged?.Invoke(layout);
        }

        public void Shift(ShiftState state)
        {
            ShiftState = state;
            ShiftChanged?.Invoke(state);
        }

        public void Backspace()
        {
            var selectionStart = Mathf.Min(SelectStartIndex, SelectEndIndex);
            var selectionEnd = Mathf.Max(SelectStartIndex, SelectEndIndex);
            var selectionLength = selectionEnd - selectionStart;

            if (selectionLength > 0)
            {
                CaretPosition = selectionStart;
                Text = Text.Remove(selectionStart, selectionLength);
            }
            else if (CaretPosition > 0)
            {
                CaretPosition--;
                Text = Text.Remove(CaretPosition, 1);
            }
        }

        public void Delete()
        {
            if (CaretPosition < Text.Length)
            {
                Text = Text.Remove(CaretPosition, 1);
            }
        }

        public void Enter()
        {
            if (_SubmitOnEnter)
            {
                Submit();
            }
            else
            {
                InsertText("\n");
            }
        }

        public void Submit()
        {
            TextSubmitted?.Invoke(Text);

            if (_CloseOnSubmit)
                Close(false);
        }

        public void Clear()
        {
            Text = string.Empty;
            CaretPosition = Text.Length;
        }

        public virtual void Open(InputField inputField, bool observeCharacterLimit = false)
        {
            Open(new NativeInputFieldWrapper(inputField), observeCharacterLimit);
        }
        
        public virtual void Open(TMP_InputField inputField, bool observeCharacterLimit = false)
        {
            Open(new TMPInputFieldWrapper(inputField), observeCharacterLimit);
        }
        
        public virtual void Open(AInputFieldWrapper inputField, bool observeCharacterLimit = false)
        {
            if (inputField != null && inputField.IsValid())
            {
                CurrentInputField = inputField;
                _monitorCharacterLimit = observeCharacterLimit;
                _characterLimit = observeCharacterLimit ? CurrentInputField.CharacterLimit : -1;
            }

            Open(CurrentInputField.Text);
        }

        public void Open()
        {
            Open(Text);
        }

        public void OpenCleared()
        {
            Open(string.Empty);
        }

        public void Open(string newText)
        {
            if (!isActiveAndEnabled)
            {
                Opened?.Invoke();
            }

            CaretPosition = newText.Length;
            Text = newText;
            gameObject.SetActive(true);
            _isOpen = true;
        }

        public void Close(bool clearText, bool resetLayout = true)
        {
            Close();

            if (clearText)
                Text = string.Empty;

            if (resetLayout)
            {
                CurrentLayout = KeyboardLayout.Characters;
                LayoutChanged?.Invoke(CurrentLayout);
            }
        }

        public void Close()
        {
            CurrentInputField = null;

            _monitorCharacterLimit = false;
            _characterLimit = -1;

            if (IsShifted)
                Shift(ShiftState.None);

            Closed?.Invoke();
            gameObject.SetActive(false);
            _isOpen = false;
        }

        private void StopObservingInputField(AInputFieldWrapper inputField)
        {
            if (inputField == null || !inputField.IsValid())
                return;
            CurrentInputField.ValueChanged += OnInputFieldValueChange;
        }

        private void StartObservingInputField(AInputFieldWrapper inputField)
        {
            if (inputField == null || !inputField.IsValid()) 
                return;
            CurrentInputField.ValueChanged -= OnInputFieldValueChange;
        }

        private void OnInputFieldValueChange(string updatedText)
        {
            CaretPosition = updatedText.Length;
            Text = updatedText;
        }
    }
}
