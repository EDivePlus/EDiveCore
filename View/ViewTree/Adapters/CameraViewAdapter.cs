// Author: František Holubec
// Created: 08.10.2026

using UnityEngine;

namespace EDIVE.View.ViewTree
{
    [ViewAdapter(typeof(Camera))]
    [RequireComponent(typeof(Camera))]
    public class CameraViewAdapter : AComponentToggleViewAdapter<Camera>
    {
        protected override void SetActive(Camera component, bool active) => component.enabled = active;
    }
}
