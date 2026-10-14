// Author: František Holubec
// Created: 14.10.2026

using UnityEngine;

namespace EDIVE.View
{
    // Own sorting order for a nested canvas. Set at runtime, a prefab canvas without a parent canvas drops override sorting.
    [RequireComponent(typeof(Canvas))]
    public class CanvasSorting : MonoBehaviour
    {
        [SerializeField]
        private int _SortingOrder;

        private void OnEnable()
        {
            var canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = _SortingOrder;
        }
    }
}
