// Author: František Holubec
// Created: 19.10.2026

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EDIVE.UIElements.ColorPicker
{
    // Reports press and release of a picker control, added at runtime
    public class ColorPickerPressRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event Action Pressed;
        public event Action Released;

        public void OnPointerDown(PointerEventData eventData) => Pressed?.Invoke();
        public void OnPointerUp(PointerEventData eventData) => Released?.Invoke();
    }
}
