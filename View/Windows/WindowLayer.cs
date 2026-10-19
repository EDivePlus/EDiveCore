// Author: František Holubec
// Created: 09.10.2026

using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.View.Windows
{
    // Draw order group of windows, e.g. frames, modals, toasts
    [CreateAssetMenu(menuName = "EDIVE/View/Window Layer", fileName = "WindowLayer")]
    public class WindowLayer : ScriptableObject
    {
        [Tooltip("Canvas sorting order of the first window, keep gaps between layers")]
        [SerializeField]
        [FormerlySerializedAs("_SortingOrder")]
        private int _BaseSortingOrder;

        [Tooltip("Showing a window here minimizes the other windows of the layer, e.g. apps")]
        [SerializeField]
        [FormerlySerializedAs("_Exclusive")]
        private bool _SingleShownWindow;

        public int SortingOrder => _BaseSortingOrder;
        public bool Exclusive => _SingleShownWindow;
    }
}
