// Author: František Holubec
// Created: 15.06.2026

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.ColorPicker
{
    public class HueSlider : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private Slider _Slider;

        [SerializeField]
        private RawImage _Background;

        [SerializeField]
        [Tooltip("Optional handle travel area. When inset from the background, the gradient ramp is " +
                 "remapped to it and the edge colors extend into the padding.")]
        private RectTransform _HandleArea;

        [SerializeField]
        [Range(8, 360)]
        private int _Resolution = 180;

        [SerializeField]
        [Tooltip("Optional graphics (e.g. the handle) tinted with the hue color at the current value.")]
        private List<Graphic> _SampleTargets = new();

        public event Action<float> HueChanged;

        private Texture2D _texture;

        public float Hue => _Slider ? _Slider.normalizedValue : 0f;

        private void Awake()
        {
            RegenerateTexture();
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
        
        public void SetHue(float hue)
        {
            if (_Slider)
                _Slider.SetValueWithoutNotify(Mathf.Lerp(_Slider.minValue, _Slider.maxValue, Mathf.Clamp01(hue)));
            ApplySampleColor();
        }

        private void OnSliderChanged(float value)
        {
            ApplySampleColor();
            HueChanged?.Invoke(_Slider.normalizedValue);
        }

        // Tints the sample targets with the fully-saturated hue under the handle.
        private void ApplySampleColor()
        {
            if (_SampleTargets == null || _SampleTargets.Count == 0)
                return;

            var color = Color.HSVToRGB(Hue, 1f, 1f);
            foreach (var target in _SampleTargets)
            {
                if (target)
                    target.color = color;
            }
        }

        private void RegenerateTexture()
        {
            if (!_Background)
                return;

            var colors = new Color32[_Resolution];
            for (var i = 0; i < _Resolution; i++)
                colors[i] = Color.HSVToRGB((float) i / (_Resolution - 1), 1f, 1f);

            var direction = _Slider ? _Slider.direction : Slider.Direction.LeftToRight;
            ColorPickerUtils.Apply(_Background, ref _texture, colors, direction);
            ColorPickerUtils.ApplyInset(_Background, _HandleArea);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_texture)
                ColorPickerUtils.ApplyInset(_Background, _HandleArea);
        }
    }
}
