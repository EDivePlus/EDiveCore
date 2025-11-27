// Author: František Holubec
// Created: 27.11.2025

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EDIVE.XRTools.Keyboard
{
    public class SelectForwarder : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public event Action GainedFocus;
        public event Action LostFocus;

        void ISelectHandler.OnSelect(BaseEventData eventData) => GainedFocus?.Invoke();
        void IDeselectHandler.OnDeselect(BaseEventData eventData) => LostFocus?.Invoke();
    }
}
