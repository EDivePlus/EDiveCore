// Author: František Holubec
// Created: 26.11.2025

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace EDIVE.Input.Keyboard.InputFieldWrappers
{
    [MovedFrom(true, "EDIVE.XRTools.Keyboard", "EDIVE.XRTools")]
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
