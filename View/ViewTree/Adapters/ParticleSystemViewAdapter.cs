// Author: František Holubec
// Created: 08.10.2026

using UnityEngine;

namespace EDIVE.View.ViewTree
{
    // Resumes only if it was playing when hidden, Play On Awake counts on the first hide
    [ViewAdapter(typeof(ParticleSystem))]
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSystemViewAdapter : AComponentToggleViewAdapter<ParticleSystem>
    {
        private bool _resume;
        private bool _wasHidden;

        protected override void SetActive(ParticleSystem component, bool active)
        {
            if (active)
            {
                if (_resume)
                    component.Play(true);
                _resume = false;
                return;
            }

            _resume = component.isPlaying || (!_wasHidden && component.main.playOnAwake);
            _wasHidden = true;
            component.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
