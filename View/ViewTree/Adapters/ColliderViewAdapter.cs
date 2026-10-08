// Author: František Holubec
// Created: 08.10.2026

using UnityEngine;

namespace EDIVE.View.ViewTree
{
    public class ColliderViewAdapter : AComponentToggleViewAdapter<Collider>
    {
        protected override ViewState DefaultActiveWhile => ViewState.Focused;

        protected override void SetActive(Collider component, bool active) => component.enabled = active;
    }
}
