// Author: František Holubec
// Created: 08.10.2026

using UnityEngine;
using UnityEngine.EventSystems;

namespace EDIVE.View.ViewTree
{
    [ViewAdapter(typeof(BaseRaycaster))]
    public class RaycasterViewAdapter : AComponentToggleViewAdapter<BaseRaycaster>
    {
        protected override ViewState DefaultActiveWhile => ViewState.Focused;

        protected override void SetActive(BaseRaycaster component, bool active) => component.enabled = active;
    }
}
