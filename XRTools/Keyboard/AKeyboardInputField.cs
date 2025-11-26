// Author: František Holubec
// Created: 26.11.2025

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.XRTools.Keyboard
{
    [Serializable]
    public abstract class AInputFieldWrapper
    {
   
        public abstract bool IsFocused { get; }
        public abstract string Text { get; set; }
        public abstract int CaretPosition { get; set; }

        public abstract event Action GainedFocus;
        public abstract event Action<string, int, int> TextSelectionChanged;
        
        public abstract bool IsValid();
        public abstract void PrepareForKeyboard();
        public abstract void UpdateText(string text, KeyboardController keyboard);
    }

    [Serializable]
    public class TMPInputFieldWrapper : AInputFieldWrapper
    {
        [SerializeField]
        private TMP_InputField _InputField;

        public override bool IsFocused => _InputField.isFocused;
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
        }

        public override void UpdateText(string text, KeyboardController keyboard)
        {
            _InputField.text = text;
            _InputField.stringPosition = keyboard.CaretPosition;
            _InputField.selectionAnchorPosition = keyboard.SelectStartIndex;
            _InputField.selectionFocusPosition = keyboard.SelectEndIndex;
        }
     }
      
    [Serializable]
    public class NativeInputFieldWrapper : AInputFieldWrapper
    {
        [SerializeField]
        private InputField _InputField;
        
        public override bool IsFocused => _InputField.isFocused;
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

        public override event Action GainedFocus
        {
            add
            {
                if (GainedFocusInternal == null)
                    ;//_InputField.onSelect.AddListener(OnSelected); 
                GainedFocusInternal += value; 
            }
            remove
            {
                GainedFocusInternal -= value;
                if (GainedFocusInternal == null)
                    ;//_InputField.onSelect.RemoveListener(OnSelected); 
            }
        }
        private event Action GainedFocusInternal;
        private void OnSelected(string text) => GainedFocusInternal?.Invoke();
        
        public override event Action<string, int, int> TextSelectionChanged
        {
            add
            {
                if (TextSelectionChangedInternal == null)
                    ;//_InputField.onTextSelection.AddListener(OnTextSelectionChanged); 
                TextSelectionChangedInternal += value; 
            }
            remove
            {
                TextSelectionChangedInternal -= value;
                if (TextSelectionChangedInternal == null)
                    ;//_InputField.onTextSelection.RemoveListener(OnTextSelectionChanged); 
            }
        }
        private event Action<string, int, int> TextSelectionChangedInternal;
        private void OnTextSelectionChanged(string selectedText, int startIndex, int endIndex) => TextSelectionChangedInternal?.Invoke(selectedText, startIndex, endIndex);

        
        public NativeInputFieldWrapper(InputField inputField)
        {
            _InputField = inputField;
        }
        
        public override bool IsValid() => _InputField != null;
        public override void PrepareForKeyboard()
        {
            _InputField.shouldHideMobileInput = true;
        }
        
        public override void UpdateText(string text, KeyboardController keyboard)
        {
            _InputField.text = text;
            _InputField.caretPosition = keyboard.CaretPosition;
            _InputField.selectionAnchorPosition = keyboard.SelectStartIndex;
            _InputField.selectionFocusPosition = keyboard.SelectEndIndex;
        }
    }
}
