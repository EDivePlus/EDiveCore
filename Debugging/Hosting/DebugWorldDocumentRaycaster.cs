// Author: František Holubec
// Created: 15.10.2026

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // The default world raycaster reports order 0, so any canvas above 0 wins the ray. Debug menu hits use the menu's sorting order instead.
    [AddComponentMenu("")]
    internal sealed class DebugWorldDocumentRaycaster : WorldDocumentRaycaster
    {
        private readonly List<RaycastResult> _hits = new();

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            _hits.Clear();
            base.Raycast(eventData, _hits);
            foreach (var hit in _hits)
            {
                if (hit.document == null || !hit.document.TryGetComponent<DebugMenuHost>(out var host))
                    continue;

                var result = hit;
                if (result.sortingOrder != int.MaxValue)
                    result.sortingOrder = host.WorldSortingOrder;
                resultAppendList.Add(result);
            }
        }
    }
}
