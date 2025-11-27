// Author: František Holubec
// Created: 26.11.2025

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.XRTools.Keyboard
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
        }
    }
      
    [Serializable]
    public class NativeInputFieldWrapper : AInputFieldWrapper
    {
        [SerializeField]
        private InputField _InputField;
        
        public override GameObject GameObject => _InputField.gameObject;
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
        public override int CharacterLimit
        {
            get => _InputField.characterLimit;
            set => _InputField.characterLimit = value;
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
                var selectForwarder = _InputField.GetOrAddComponent<SelectForwarder>();
                selectForwarder.GainedFocus += value; 
            }
            remove
            {
                if (_InputField.TryGetComponent<SelectForwarder>(out var selectForwarder)) 
                    selectForwarder.GainedFocus -= value; 
            }
        }
        
        public override event Action<string, int, int> TextSelectionChanged
        {
            add
            {
                if (TextSelectionChangedInternal == null)
                {
                    _textSelectionWatchCts?.Cancel();
                    _textSelectionWatchCts?.Dispose();
                    _textSelectionWatchCts = new CancellationTokenSource();
                    WatchForTextSelection(_textSelectionWatchCts.Token).Forget();
                }
                TextSelectionChangedInternal += value; 
            }
            remove
            {
                TextSelectionChangedInternal -= value;
                if (TextSelectionChangedInternal == null)
                {
                    _textSelectionWatchCts?.Cancel();
                    _textSelectionWatchCts?.Dispose();
                    _textSelectionWatchCts = null;
                }
            }
        }
        private CancellationTokenSource _textSelectionWatchCts;
        private event Action<string, int, int> TextSelectionChangedInternal;
        
        public NativeInputFieldWrapper(InputField inputField)
        {
            _InputField = inputField;
        }
        
        private async UniTaskVoid WatchForTextSelection(CancellationToken cancellationToken)
        {
            if (_InputField == null) 
                return;
            
            var lastAnchor = _InputField.selectionAnchorPosition;
            var lastFocus  = _InputField.selectionFocusPosition;

            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                if (_InputField == null) 
                    break;

                var anchor = _InputField.selectionAnchorPosition;
                var focus  = _InputField.selectionFocusPosition;

                if (anchor == lastAnchor && focus == lastFocus) 
                    continue;
                
                lastAnchor = anchor;
                lastFocus  = focus;

                var selected = "";
                if (anchor != focus && _InputField.text.Length > 0)
                {
                    var start = Mathf.Min(anchor, focus);
                    var end   = Mathf.Max(anchor, focus);
                    selected  = _InputField.text.Substring(start, end - start);
                }
                TextSelectionChangedInternal?.Invoke(selected, anchor, focus);
            }
        }
        
        public override bool IsValid() => _InputField != null;
        public override void PrepareForKeyboard()
        {
            _InputField.shouldHideMobileInput = true;
        }
    }
}
