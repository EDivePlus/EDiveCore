// Author: František Holubec
// Created: 15.06.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.ColorPicker
{
    public class ColorPickerController : MonoBehaviour
    {
        [SerializeField]
        private SaturationValueField _SaturationValueField;

        [SerializeField]
        private HueSlider _HueSlider;

        [SerializeField]
        private HexColorInput _HexInput;

        [SerializeField]
        private List<Graphic> _ColorPreviews = new();

        [SerializeField]
        private List<ColorChannelSlider> _ChannelSliders = new();

        [SerializeField]
        private List<ColorChannelLabel> _ChannelLabels = new();

        [PropertySpace]
        [SerializeField]
        private Color _DefaultColor = Color.red;
        
        public event Action<Color> ColorChanged;

        private float _hue;
        private float _saturation = 1f;
        private float _value = 1f;
        private float _alpha = 1f;
        
        [ShowInInspector]
        [HideInEditorMode]
        public Color Color
        {
            get => Color.HSVToRGB(_hue, _saturation, _value).WithA(_alpha);
            set
            {
                Color.RGBToHSV(value, out _hue, out _saturation, out _value);
                _alpha = value.a;
                RefreshComponents();
            }
        }

        private void OnEnable()
        {
            if (_SaturationValueField)
                _SaturationValueField.ValueChanged += OnSaturationValueChanged;
            if (_HueSlider)
                _HueSlider.HueChanged += OnHueChanged;
            if (_HexInput)
                _HexInput.ColorSubmitted += OnHexSubmitted;
            foreach (var slider in _ChannelSliders)
            {
                if (slider)
                    slider.ValueChanged += OnChannelChanged;
            }
        }

        private void OnDisable()
        {
            if (_SaturationValueField)
                _SaturationValueField.ValueChanged -= OnSaturationValueChanged;
            if (_HueSlider)
                _HueSlider.HueChanged -= OnHueChanged;
            if (_HexInput)
                _HexInput.ColorSubmitted -= OnHexSubmitted;
            foreach (var slider in _ChannelSliders)
            {
                if (slider)
                    slider.ValueChanged -= OnChannelChanged;
            }
        }
        
        private void Start()
        {
            Color = _DefaultColor;
        }

        private void OnHueChanged(float hue)
        {
            _hue = hue;
            if (_SaturationValueField)
                _SaturationValueField.SetHue(_hue);
            PushOutputs();
        }

        private void OnSaturationValueChanged(float saturation, float value)
        {
            _saturation = saturation;
            _value = value;
            PushOutputs();
        }

        private void OnHexSubmitted(Color color)
        {
            Color = color;
        }

        private void OnChannelChanged(ColorChannel channel, float value)
        {
            SetChannel(channel, value);
            RefreshComponents();
        }

        private void RefreshComponents()
        {
            if (_HueSlider)
                _HueSlider.SetHue(_hue);
            if (_SaturationValueField)
            {
                _SaturationValueField.SetHue(_hue);
                _SaturationValueField.SetSaturationValue(_saturation, _value);
            }
            PushOutputs();
        }

        private void PushOutputs()
        {
            var color = Color;
            if (_HexInput)
                _HexInput.SetColor(color);
            _ColorPreviews.Where(p => p != null).ForEach(p => p.color = color);
            UpdateChannelWidgets(color);
            ColorChanged?.Invoke(color);
        }

        private void UpdateChannelWidgets(Color color)
        {
            foreach (var slider in _ChannelSliders)
            {
                if (!slider)
                    continue;
                slider.SetValue(GetChannel(slider.Channel, color));
                if (TryGetGradient(slider.Channel, color, out var from, out var to))
                    slider.SetGradient(from, to);
            }

            foreach (var label in _ChannelLabels)
            {
                if (label)
                    label.SetValue(GetChannel(label.Channel, color));
            }
        }

        private float GetChannel(ColorChannel channel, Color color)
        {
            return channel switch
            {
                ColorChannel.R => color.r,
                ColorChannel.G => color.g,
                ColorChannel.B => color.b,
                ColorChannel.A => _alpha,
                ColorChannel.Hue => _hue,
                ColorChannel.Saturation => _saturation,
                ColorChannel.Value => _value,
                _ => 0f
            };
        }

        private void SetChannel(ColorChannel channel, float value)
        {
            switch (channel)
            {
                case ColorChannel.R:
                    ApplyRgb(Color.WithR(value));
                    break;
                case ColorChannel.G:
                    ApplyRgb(Color.WithG(value));
                    break;
                case ColorChannel.B:
                    ApplyRgb(Color.WithB(value));
                    break;
                case ColorChannel.A:
                    _alpha = value;
                    break;
                case ColorChannel.Hue:
                    _hue = value;
                    break;
                case ColorChannel.Saturation:
                    _saturation = value;
                    break;
                case ColorChannel.Value:
                    _value = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }

        private void ApplyRgb(Color color)
        {
            Color.RGBToHSV(color, out _hue, out _saturation, out _value);
            _alpha = color.a;
        }
        
        private bool TryGetGradient(ColorChannel channel, Color color, out Color from, out Color to)
        {
            switch (channel)
            {
                case ColorChannel.R:
                    from = new Color(0f, color.g, color.b);
                    to = new Color(1f, color.g, color.b);
                    return true;
                case ColorChannel.G:
                    from = new Color(color.r, 0f, color.b);
                    to = new Color(color.r, 1f, color.b);
                    return true;
                case ColorChannel.B:
                    from = new Color(color.r, color.g, 0f);
                    to = new Color(color.r, color.g, 1f);
                    return true;
                case ColorChannel.A:
                    from = new Color(color.r, color.g, color.b, 0f);
                    to = new Color(color.r, color.g, color.b, 1f);
                    return true;
                case ColorChannel.Saturation:
                    from = Color.HSVToRGB(_hue, 0f, _value);
                    to = Color.HSVToRGB(_hue, 1f, _value);
                    return true;
                case ColorChannel.Value:
                    from = Color.HSVToRGB(_hue, _saturation, 0f);
                    to = Color.HSVToRGB(_hue, _saturation, 1f);
                    return true;
                case ColorChannel.Hue:
                default:
                    from = to = default;
                    return false;
            }
        }
    }
}
