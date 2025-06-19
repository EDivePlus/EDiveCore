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

        public KeyboardController Keyboard => _Keyboard;

        private void Awake()
        {
            if (_HideKeyboardOnAwake)
            {
                _Keyboard.gameObject.SetActive(false);
            }
        }
    }
}
