// Author: František Holubec
// Created: 08.10.2026

using UnityEngine;

namespace EDIVE.View.ViewTree
{
    [ViewAdapter(typeof(Animator))]
    [RequireComponent(typeof(Animator))]
    public class AnimatorViewAdapter : AComponentToggleViewAdapter<Animator>
    {
        [SerializeField]
        [Tooltip("Keep animator state while hidden.")]
        private bool _KeepState = true;

        protected override void SetActive(Animator component, bool active)
        {
            if (!active && _KeepState)
                component.keepAnimatorStateOnDisable = true;
            component.enabled = active;
        }
    }
}
