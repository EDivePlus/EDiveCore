// Author: František Holubec
// Created: 08.10.2026

using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.View.ViewTree
{
    [ViewAdapter(typeof(Animator))]
    [RequireComponent(typeof(Animator))]
    public class AnimatorViewAdapter : AComponentToggleViewAdapter<Animator>
    {
        [SerializeField]
        [Tooltip("Keep animator state while hidden.")]
        [FormerlySerializedAs("_KeepState")]
        private bool _KeepAnimatorState = true;

        protected override void SetActive(Animator component, bool active)
        {
            if (!active && _KeepAnimatorState)
                component.keepAnimatorStateOnDisable = true;
            component.enabled = active;
        }
    }
}
