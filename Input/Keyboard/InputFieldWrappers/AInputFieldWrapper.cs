// Author: František Holubec
// Created: 07.05.2026

using System;
using UnityEngine;

namespace EDIVE.Input.Keyboard.InputFieldWrappers
{
    [Serializable]
    public abstract class AInputFieldWrapper
    {
        public abstract GameObject GameObject { get; }
        public abstract bool IsFocused { get; }
        public abstract string Text { get; set; }
        public abstract int CaretPosition { get; set; }
        public abstract int CharacterLimit { get; set; }
        public abstract int SelectionAnchorPosition { get; set; }
        public abstract int SelectionFocusPosition { get; set; }

        public abstract event Action<string> ValueChanged;
        public abstract event Action GainedFocus;
        public abstract event Action<string, int, int> TextSelectionChanged;
        
        public abstract bool IsValid();
        public abstract void PrepareForKeyboard();
    }
}
