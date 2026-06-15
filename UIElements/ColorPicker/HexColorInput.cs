// Author: František Holubec
// Created: 15.06.2026

using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.UIElements.ColorPicker
{
    public class HexColorInput : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private TMP_InputField _InputField;

        [SerializeField]
        private bool _IncludeAlpha;
        
        public event Action<Color> ColorSubmitted;

        private Color _lastValid = Color.white;

        private void Awake()
        {
            if (_InputField)
                _InputField.onEndEdit.AddListener(OnEndEdit);
        }

        private void OnDestroy()
        {
            if (_InputField)
                _InputField.onEndEdit.RemoveListener(OnEndEdit);
        }
        
        public void SetColor(Color color)
        {
            _lastValid = color;
            if (_InputField)
                _InputField.SetTextWithoutNotify(ColorToHex(color));
        }

        private void OnEndEdit(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                hex = string.Empty;
            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            if (ColorUtility.TryParseHtmlString(hex, out var color))
            {
                if (!_IncludeAlpha)
                    color.a = _lastValid.a;
                _lastValid = color;
                ColorSubmitted?.Invoke(color);
            }
            else
            {
                SetColor(_lastValid);
            }
        }

        private string ColorToHex(Color color)
        {
            var color32 = (Color32) color;
            return _IncludeAlpha
                ? $"#{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}"
                : $"#{color32.r:X2}{color32.g:X2}{color32.b:X2}";
        }
    }
}
