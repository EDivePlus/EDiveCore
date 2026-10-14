// Author: František Holubec
// Created: 09.10.2026

using UnityEngine;

namespace EDIVE.View.Windows
{
    // Draw order group of windows, e.g. frames, modals, toasts
    [CreateAssetMenu(menuName = "EDIVE/View/Window Layer", fileName = "WindowLayer")]
    public class WindowLayer : ScriptableObject
    {
        // Canvas sorting order of the first window, keep gaps between layers
        [SerializeField]
        private int _SortingOrder;

        // Showing a window here minimizes the other windows of the layer, e.g. apps
        [SerializeField]
        private bool _Exclusive;

        public int SortingOrder => _SortingOrder;
        public bool Exclusive => _Exclusive;
    }
}
