// Author: František Holubec
// Created: 15.06.2026

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.ColorPicker
{
    public class ColorChannelSlider : MonoBehaviour
    {
        [SerializeField]
        private ColorChannel _Channel;

        [SerializeField]
        [Required]
        private Slider _Slider;

        [SerializeField]
        [Tooltip("Optional background tinted with a 2-stop gradient of this channel's range.")]
        private RawImage _Gradient;

        [SerializeField]
        [Tooltip("Optional handle travel area. When inset from the gradient, the ramp is remapped to it " +
                 "and the edge colors extend into the padding.")]
        private RectTransform _HandleArea;

        [SerializeField]
        [Tooltip("Optional graphics (e.g. the handle) tinted with the gradient color at the current value.")]
        private List<Graphic> _SampleTargets = new();

        public event Action<ColorChannel, float> ValueChanged;

        public ColorChannel Channel => _Channel;

        private Texture2D _texture;
        private Color _from = Color.black;
        private Color _to = Color.white;

        private void Awake()
        {
            if (_Slider)
                _Slider.onValueChanged.AddListener(OnSliderChanged);
        }

        private void OnDestroy()
        {
            if (_Slider)
                _Slider.onValueChanged.RemoveListener(OnSliderChanged);
            if (_texture)
                Destroy(_texture);
        }
        
        public void SetValue(float normalized)
        {
            if (_Slider)
                _Slider.SetValueWithoutNotify(Mathf.Lerp(_Slider.minValue, _Slider.maxValue, Mathf.Clamp01(normalized)));
            ApplySampleColor();
        }

        public void SetGradient(Color from, Color to)
        {
            _from = from;
            _to = to;

            if (_Gradient)
            {
                var direction = _Slider ? _Slider.direction : Slider.Direction.LeftToRight;
                ColorPickerUtils.Apply(_Gradient, ref _texture, new Color32[] {from, to}, direction);
                ColorPickerUtils.ApplyInset(_Gradient, _HandleArea);
            }

            ApplySampleColor();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_texture)
                ColorPickerUtils.ApplyInset(_Gradient, _HandleArea);
        }

        private void OnSliderChanged(float value)
        {
            ApplySampleColor();
            ValueChanged?.Invoke(_Channel, _Slider.normalizedValue);
        }

        // Tints the sample targets with the gradient color under the handle.
        private void ApplySampleColor()
        {
            if (_SampleTargets == null || _SampleTargets.Count == 0)
                return;

            var color = Color.Lerp(_from, _to, _Slider ? _Slider.normalizedValue : 0f);
            foreach (var target in _SampleTargets)
            {
                if (target)
                    target.color = color;
            }
        }
    }
}
