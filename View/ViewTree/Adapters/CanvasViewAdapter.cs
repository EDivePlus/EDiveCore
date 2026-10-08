// Author: František Holubec
// Created: 08.10.2026

using UnityEngine;

namespace EDIVE.View.ViewTree
{
    [ViewAdapter(typeof(Canvas))]
    [RequireComponent(typeof(Canvas))]
    public class CanvasViewAdapter : AComponentToggleViewAdapter<Canvas>
    {
        protected override void SetActive(Canvas component, bool active) => component.enabled = active;
    }
}
