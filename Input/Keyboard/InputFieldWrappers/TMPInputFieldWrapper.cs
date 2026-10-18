// Author: František Holubec
// Created: 07.05.2026

using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EDIVE.Input.Keyboard.InputFieldWrappers
{
    [MovedFrom(true, "EDIVE.XRTools.Keyboard", "EDIVE.XRTools")]
    [Serializable]
    public class TMPInputFieldWrapper : AInputFieldWrapper
    {
        [SerializeField]
        private TMP_InputField _InputField;
        
        public override GameObject GameObject => _InputField.gameObject;
        public override bool IsFocused => _InputField.isFocused;
        public override int CharacterLimit
        {
            get => _InputField.characterLimit;
            set => _InputField.characterLimit = value;
        }
        public override string Text
        {
            get => _InputField.text;
            set => _InputField.text = value;
        }
        public override int CaretPosition
        {
            get => _InputField.caretPosition;
            set => _InputField.caretPosition = value;
        }
        public override int SelectionAnchorPosition
        {
            get => _InputField.selectionAnchorPosition;
            set => _InputField.selectionAnchorPosition = value;
        }
        public override int SelectionFocusPosition
        {
            get => _InputField.selectionFocusPosition;
            set => _InputField.selectionFocusPosition = value;
        }
        public override bool IsEditable => _InputField.IsInteractable() && !_InputField.readOnly;
        public override bool IsMultiLine => _InputField.multiLine;
        public override string Placeholder => _InputField.placeholder is TMP_Text placeholder ? placeholder.text : null;
        public override bool IsPassword
        {
            get => _InputField.contentType == TMP_InputField.ContentType.Password;
            set
            {
                if (IsPassword == value)
                    return;

                _InputField.contentType = value ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
                _InputField.ForceLabelUpdate();
            }
        }

        private static readonly MethodInfo VALIDATE_METHOD = typeof(TMP_InputField).GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] {typeof(string), typeof(int), typeof(char)}, null);

        public override event Action<string> ValueChanged
        {
            add
            {
                if (ValueChangedInternal == null)
                    _InputField.onValueChanged.AddListener(OnValueChanged); 
                ValueChangedInternal += value; 
            }
            remove
            {
                ValueChangedInternal -= value; 
                if (ValueChangedInternal == null)
                    _InputField.onValueChanged.RemoveListener(OnValueChanged); 
            }
        }
        private event Action<string> ValueChangedInternal;
        private void OnValueChanged(string text) => ValueChangedInternal?.Invoke(text);
        
        public override event Action GainedFocus
        {
            add
            {
                if (GainedFocusInternal == null)
                    _InputField.onSelect.AddListener(OnSelected); 
                GainedFocusInternal += value; 
            }
            remove
            {
                GainedFocusInternal -= value; 
                if (GainedFocusInternal == null)
                    _InputField.onSelect.RemoveListener(OnSelected); 
            }
        }
        private event Action GainedFocusInternal;
        private void OnSelected(string text) => GainedFocusInternal?.Invoke();
        
        public override event Action<string, int, int> TextSelectionChanged
        {
            add
            {
                if (TextSelectionChangedInternal == null)
                    _InputField.onTextSelection.AddListener(OnTextSelectionChanged); 
                TextSelectionChangedInternal += value; 
            }
            remove
            {
                TextSelectionChangedInternal -= value; 
                if (TextSelectionChangedInternal == null)
                    _InputField.onTextSelection.RemoveListener(OnTextSelectionChanged); 
            }
        }
        private event Action<string, int, int> TextSelectionChangedInternal;
        private void OnTextSelectionChanged(string selectedText, int startIndex, int endIndex) => TextSelectionChangedInternal?.Invoke(selectedText, startIndex, endIndex);
        
        public TMPInputFieldWrapper(TMP_InputField inputField)
        {
            _InputField = inputField;
        }

        public override bool IsValid() => _InputField != null;
        
        public override void PrepareForKeyboard()
        {
            _InputField.resetOnDeActivation = false;
            _InputField.shouldHideSoftKeyboard = true;
            // Keys append at the caret, a select all on focus would look like the text gets replaced
            _InputField.onFocusSelectAll = false;
        }

        public override char Validate(string text, int position, char character)
        {
            if (_InputField.onValidateInput != null)
                return _InputField.onValidateInput(text, position, character);

            if (_InputField.characterValidation == TMP_InputField.CharacterValidation.None || VALIDATE_METHOD == null)
                return character;

            return (char) VALIDATE_METHOD.Invoke(_InputField, new object[] {text, position, character});
        }

        public override void Submit() => _InputField.onSubmit.Invoke(_InputField.text);
    }
}
