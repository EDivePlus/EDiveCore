// Author: František Holubec
// Created: 26.06.2025

using EDIVE.XRTools.Keyboard;
using UnityEngine;

namespace EDIVE.XRTools.Tablet
{
    public class TabletController : MonoBehaviour
    {
        [SerializeField]
        private KeyboardController _Keyboard;

        [SerializeField]
        private Canvas _MainLayer;

        [SerializeField]
        private Canvas _OverlayLayer;

        [SerializeField]
        private Canvas _DebugLayer;

        public KeyboardController Keyboard => _Keyboard;
        public Canvas MainLayer => _MainLayer;
        public Canvas OverlayLayer => _OverlayLayer;
        public Canvas DebugLayer => _DebugLayer;
    }
}
