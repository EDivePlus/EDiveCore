// Author: František Holubec
// Created: 08.10.2026

using UnityEngine;

namespace EDIVE.View.ViewTree
{
    public class RendererViewAdapter : AComponentToggleViewAdapter<Renderer>
    {
        protected override void SetActive(Renderer component, bool active) => component.enabled = active;
    }
}
