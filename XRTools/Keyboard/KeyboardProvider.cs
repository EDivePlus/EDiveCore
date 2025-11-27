// Author: František Holubec
// Created: 18.06.2025

using UnityEngine;

namespace EDIVE.XRTools.Keyboard
{
    public class KeyboardProvider : MonoBehaviour
    {
        [SerializeField]
        private KeyboardController _Keyboard;

        [SerializeField]
        private bool _HideKeyboardOnAwake = true;

        public KeyboardController Keyboard
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
