// Author: František Holubec
// Created: 18.06.2025

using UnityEngine;

namespace EDIVE.Input.Keyboard
{
    public class VirtualKeyboardProvider : MonoBehaviour
    {
        [SerializeField]
        private VirtualKeyboardController _Keyboard;

        [SerializeField]
        private bool _HideKeyboardOnAwake = true;

        public VirtualKeyboardController Keyboard
        {
            get => _Keyboard;
            set => _Keyboard = value;
        }

        private void Awake()
        {
            if (_Keyboard != null && _HideKeyboardOnAwake)
            {
                _Keyboard.gameObject.SetActive(false);
            }
        }
    }
}
