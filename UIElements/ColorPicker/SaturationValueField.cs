// Author: František Holubec
// Created: 15.06.2026

using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDIVE.UIElements.ColorPicker
{
    public class SaturationValueField : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField]
        [Required]
        private RawImage _Image;

        [SerializeField]
        [Required]
        private RectTransform _Handle;

        [SerializeField]
        [Tooltip("Area the handle travels within. Inset it from the gradient so the handle stays inside the box. " +
                 "Defaults to this transform when unassigned.")]
        private RectTransform _HandleArea;

        [SerializeField]
        [Range(8, 256)]
        private int _Resolution = 128;
        
        public event Action<float, float> ValueChanged;

        private float _hue;
        private float _saturation;
        private float _value;
        private float _generatedHue = -1f;
        private Texture2D _texture;

        private RectTransform HandleArea => _HandleArea ? _HandleArea : (RectTransform) transform;

        private void OnEnable() => RegenerateTexture();

        private void OnDestroy()
        {
            if (_texture)
                Destroy(_texture);
        }
        
        public void SetHue(float hue)
        {
            _hue = hue;
            if (!Mathf.Approximately(_generatedHue, hue))
                RegenerateTexture();
        }
        
        public void SetSaturationValue(float saturation, float value)
        {
            _saturation = Mathf.Clamp01(saturation);
            _value = Mathf.Clamp01(value);
            UpdateHandle();
        }

        public void OnPointerDown(PointerEventData eventData) => UpdateFromPointer(eventData);

        public void OnDrag(PointerEventData eventData) => UpdateFromPointer(eventData);

        private void UpdateFromPointer(PointerEventData eventData)
        {
            var area = HandleArea;
            var rect = area.rect;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out var local))
                return;

            _saturation = rect.width > 0 ? Mathf.Clamp01((local.x - rect.x) / rect.width) : 0f;
            _value = rect.height > 0 ? Mathf.Clamp01((local.y - rect.y) / rect.height) : 0f;

            UpdateHandle();
            ValueChanged?.Invoke(_saturation, _value);
        }

        private void UpdateHandle()
        {
            if (!_Handle)
                return;

            var anchor = new Vector2(_saturation, _value);
            _Handle.anchorMin = anchor;
            _Handle.anchorMax = anchor;
            _Handle.anchoredPosition = Vector2.zero;
        }

        private void RegenerateTexture()
        {
            if (!_Image)
                return;

            _generatedHue = _hue;
            if (_texture == null || _texture.width != _Resolution)
            {
                if (_texture)
                    Destroy(_texture);
                _texture = new Texture2D(_Resolution, _Resolution) {wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave};
            }

            var colors = new Color32[_Resolution * _Resolution];
            for (var s = 0; s < _Resolution; s++)
            {
                for (var v = 0; v < _Resolution; v++)
                {
                    colors[v * _Resolution + s] = Color.HSVToRGB(_hue, (float) s / (_Resolution - 1), (float) v / (_Resolution - 1));
                }
            }

            _texture.SetPixels32(colors);
            _texture.Apply();
            _Image.texture = _texture;
            ColorPickerUtils.ApplyInset(_Image, _HandleArea);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_texture)
                ColorPickerUtils.ApplyInset(_Image, _HandleArea);
        }
    }
}