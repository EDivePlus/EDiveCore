// Author: František Holubec
// Created: 18.06.2025

using UnityEngine;

namespace EDIVE.XRTools.Keyboard
{
    public class KeyboardProvider : MonoBehaviour
    {
        [SerializeField]
        private KeyboardController _Keyboard;
        public KeyboardController Keyboard => _Keyboard;
    }
}
