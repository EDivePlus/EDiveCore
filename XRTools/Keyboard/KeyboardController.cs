using System;
using System.Collections.Generic;
using EDIVE.External.Signals;
using EDIVE.StateHandling.MultiStates;
using TMPro;
using UnityEngine;

namespace EDIVE.XRTools.Keyboard
{
    public class KeyboardController : MonoBehaviour
    {
        [SerializeField]
        private bool _SubmitOnEnter = true;

        [SerializeField]
        private bool _CloseOnSubmit = true;

        [SerializeField]
        [ValidateMultiStateWithEnum(typeof(KeyboardLayout))]
        private AMultiState _LayoutState;

        public TMP_InputField CurrentInputField
        {
            get => _currentInputField;
            set
            {
                if (_currentInputField == value)
                    return;

                StopObservingInputField(_currentInputField);
                _currentInputField = value;
                StartObservingInputField(_currentInputField);

                FocusChanged.Dispatch();
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
                TextUpdated.Dispatch(_text);
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

        public Signal Opened { get; } = new();
        public Signal Closed { get; } = new();
        public Signal<KeyboardKey> KeyPressed { get; } = new();
        public Signal<KeyboardLayout> LayoutChanged { get; } = new();
        public Signal<ShiftState> ShiftChanged { get; } = new();
        public Signal<string> TextUpdated { get; } = new();
        public Signal<string> TextSubmitted { get; } = new();
        public Signal FocusChanged { get; } = new();
        public Signal CharacterLimitReached { get; } = new();

        private TMP_InputField _currentInputField;
        private List<KeyboardKey> _keys;
        private string _text = string.Empty;

        private bool _isOpen;
        private int _characterLimit;
        private bool _monitorCharacterLimit;
        private int _caretPosition;

        private void Awake()
        {
            _keys = new List<KeyboardKey>();
            GetComponentsInChildren(true, _keys);
            _keys.ForEach(key => key.Initialize(this));
            SetLayout(KeyboardLayout.Characters);
        }

        private void OnDisable()
        {
            _isOpen = false;
        }

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
                CharacterLimitReached.Dispatch();
            }

            if (ShiftState == ShiftState.Shift)
                Shift(ShiftState.None);
        }

        public void SetLayout(KeyboardLayout layout)
        {
            CurrentLayout = layout;
            if (_LayoutState)
                _LayoutState.SetState(layout);
            LayoutChanged.Dispatch(layout);
        }

        public void Shift(ShiftState state)
        {
            ShiftState = state;
            ShiftChanged.Dispatch(state);
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
            TextSubmitted.Dispatch(Text);

            if (_CloseOnSubmit)
                Close(false);
        }

        public void Clear()
        {
            Text = string.Empty;
            CaretPosition = Text.Length;
        }

        public virtual void Open(TMP_InputField inputField, bool observeCharacterLimit = false)
        {
            CurrentInputField = inputField;
            _monitorCharacterLimit = observeCharacterLimit;
            _characterLimit = observeCharacterLimit ? CurrentInputField.characterLimit : -1;

            Open(CurrentInputField.text);
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
                Opened.Dispatch();
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
                LayoutChanged.Dispatch(CurrentLayout);
            }
        }

        public void Close()
        {
            CurrentInputField = null;

            _monitorCharacterLimit = false;
            _characterLimit = -1;

            if (IsShifted)
                Shift(ShiftState.None);

            Closed.Dispatch();
            gameObject.SetActive(false);
            _isOpen = false;
        }

        private void StopObservingInputField(TMP_InputField inputField)
        {
            if (!inputField) return;
            CurrentInputField.onValueChanged.RemoveListener(OnInputFieldValueChange);
        }

        private void StartObservingInputField(TMP_InputField inputField)
        {
            if (!inputField) return;
            CurrentInputField.onValueChanged.AddListener(OnInputFieldValueChange);
        }

        private void OnInputFieldValueChange(string updatedText)
        {
            CaretPosition = updatedText.Length;
            Text = updatedText;
        }
    }
}
