using System;
using System.Collections.Generic;
using EDIVE.Input.Keyboard.InputFieldWrappers;
using EDIVE.StateHandling.MultiStates;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDIVE.Input.Keyboard
{
    public class VirtualKeyboardController : MonoBehaviour
    {
        [SerializeField]
        private bool _SubmitOnEnter = true;

        [SerializeField]
        private bool _CloseOnSubmit = true;

        // Like on phones, a tap on anything that is not a field or the keyboard hides it
        [SerializeField]
        private bool _CloseOnFocusLost = true;

        [SerializeField]
        [ValidateMultiState(typeof(KeyboardLayout))]
        private AMultiState _LayoutState;

        [SerializeField]
        private TMP_Text _LabelText;

        [SerializeField]
        private TMP_Text _PlaceholderText;

        // Name of the field being filled, hidden when empty
        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                if (_LabelText == null)
                    return;

                _LabelText.text = value ?? string.Empty;
                _LabelText.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }

        // Placeholder of the field being filled, the keyboard's own one when empty
        public string Placeholder
        {
            set
            {
                if (_PlaceholderText != null)
                    _PlaceholderText.text = string.IsNullOrEmpty(value) ? _defaultPlaceholder : value;
            }
        }

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
                RefreshClearKeys();
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
        private bool HasValidInputField => _currentInputField != null && _currentInputField.IsValid();

        public event Action Opened;
        public event Action Closed;
        public event Action<VirtualKeyboardKey> KeyPressed;
        // On press down, also on every hold repeat, e.g. for haptics
        public event Action<VirtualKeyboardKey, PointerEventData> KeyDown;
        public event Action<KeyboardLayout> LayoutChanged;
        public event Action<ShiftState> ShiftChanged;
        public event Action<string> TextUpdated;
        public event Action<string> TextSubmitted;
        public event Action FocusChanged;
        public event Action CharacterLimitReached;

        private AInputFieldWrapper _currentInputField;
        private List<VirtualKeyboardKey> _keys;
        private string _text = string.Empty;
        private string _label;
        private string _defaultPlaceholder;
        private GameObject _lastSelected;

        private bool _isOpen;
        private int _characterLimit;
        private bool _monitorCharacterLimit;
        private int _caretPosition;

        private void Awake()
        {
            if (_PlaceholderText != null)
                _defaultPlaceholder = _PlaceholderText.text;

            _keys = new List<VirtualKeyboardKey>();
            GetComponentsInChildren(true, _keys);
            _keys.ForEach(key => key.Initialize(this));
            SetLayout(KeyboardLayout.Characters);
            RefreshClearKeys();
        }

        private void OnDisable()
        {
            _isOpen = false;
        }

        private void Update()
        {
            if (!_CloseOnFocusLost || !IsOpen)
                return;

            var selected = CurrentSelected;
            if (selected == _lastSelected)
                return;

            _lastSelected = selected;
            if (selected == null || !selected.transform.IsChildOf(transform) && !IsInputField(selected))
                Close(false);
        }

        private static GameObject CurrentSelected => UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        private static bool IsInputField(GameObject target) => target.TryGetComponent<TMP_InputField>(out _) || target.TryGetComponent<InputField>(out _);

        public void RaiseKeyPressed(VirtualKeyboardKey key) => KeyPressed?.Invoke(key);
        public void RaiseKeyDown(VirtualKeyboardKey key, PointerEventData eventData) => KeyDown?.Invoke(key, eventData);

        // Nothing to clear in an empty field
        private void RefreshClearKeys()
        {
            if (_keys == null)
                return;

            foreach (var key in _keys)
            {
                if (key.KeyType == KeyType.Clear)
                    key.gameObject.SetActive(!string.IsNullOrEmpty(_text));
            }
        }

        public virtual void InsertText(string newText)
        {
            var selectionStart = Mathf.Min(SelectStartIndex, SelectEndIndex);
            var selectionEnd = Mathf.Max(SelectStartIndex, SelectEndIndex);
            var selectionLength = selectionEnd - selectionStart;

            if (selectionLength > 0)
                CaretPosition = Mathf.Clamp(selectionStart, 0, Text.Length);

            var updatedText = Text.Remove(selectionStart, selectionLength);
            var inserted = 0;
            foreach (var character in newText)
            {
                var validated = HasValidInputField ? _currentInputField.Validate(updatedText, CaretPosition + inserted, character) : character;
                if (validated == '\0')
                    continue;

                updatedText = updatedText.Insert(CaretPosition + inserted, validated.ToString());
                inserted++;
            }

            var isUpdatedTextWithinLimits = !_monitorCharacterLimit || updatedText.Length <= _characterLimit;
            if (!isUpdatedTextWithinLimits)
            {
                CharacterLimitReached?.Invoke();
            }
            else if (inserted > 0)
            {
                CaretPosition += inserted;
                Text = updatedText;
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
            if (_SubmitOnEnter && !(HasValidInputField && _currentInputField.IsMultiLine))
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

            if (HasValidInputField)
                _currentInputField.Submit();

            if (_CloseOnSubmit)
                Close(false);
        }

        public void Clear()
        {
            Text = string.Empty;
            CaretPosition = Text.Length;
        }

        // Edits made outside the keys, e.g. a physical keyboard in the preview field
        public void SetText(string newText)
        {
            Text = newText ?? string.Empty;
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
            Label = null;
            if (inputField != null && inputField.IsValid())
            {
                CurrentInputField = inputField;
                _characterLimit = CurrentInputField.CharacterLimit;
                _monitorCharacterLimit = observeCharacterLimit || _characterLimit > 0;
            }

            Placeholder = HasValidInputField ? CurrentInputField.Placeholder : null;
            Open(CurrentInputField?.Text ?? string.Empty);
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
                SetLayout(KeyboardLayout.Characters);
                Opened?.Invoke();
            }

            CaretPosition = newText.Length;
            Text = newText;
            _lastSelected = CurrentSelected;
            gameObject.SetActive(true);
            _isOpen = true;
        }

        public void Close(bool clearText, bool resetLayout = true)
        {
            Close();

            if (clearText)
                Text = string.Empty;

            if (resetLayout)
                SetLayout(KeyboardLayout.Characters);
        }

        public void Close()
        {
            // Keys never take the selection, so the field would stay selected and a tap on it would not reopen
            var field = CurrentInputField;
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (field != null && field.IsValid() && eventSystem != null && eventSystem.currentSelectedGameObject == field.GameObject)
                eventSystem.SetSelectedGameObject(null);

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

            inputField.ValueChanged -= OnInputFieldValueChange;
        }

        private void StartObservingInputField(AInputFieldWrapper inputField)
        {
            if (inputField == null || !inputField.IsValid())
                return;

            inputField.ValueChanged += OnInputFieldValueChange;
        }

        // Only outside edits, the echo of our own text must not move the caret
        private void OnInputFieldValueChange(string updatedText)
        {
            if (updatedText == _text)
                return;

            var fieldCaret = _currentInputField != null && _currentInputField.IsValid() ? _currentInputField.CaretPosition : updatedText.Length;
            CaretPosition = Math.Clamp(fieldCaret, 0, updatedText.Length);
            Text = updatedText;
        }
    }
}
