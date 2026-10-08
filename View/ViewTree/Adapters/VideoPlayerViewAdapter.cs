// Author: František Holubec
// Created: 09.10.2026

using UnityEngine;
using UnityEngine.Video;

namespace EDIVE.View.ViewTree
{
    // Pauses while hidden, resumes only if it was playing, Play On Awake counts on the first hide
    [ViewAdapter(typeof(VideoPlayer))]
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoPlayerViewAdapter : AComponentToggleViewAdapter<VideoPlayer>
    {
        private bool _resume;
        private bool _wasHidden;

        protected override void SetActive(VideoPlayer component, bool active)
        {
            if (active)
            {
                if (_resume)
                    component.Play();
                _resume = false;
                return;
            }

            _resume = component.isPlaying || (!_wasHidden && component.playOnAwake);
            _wasHidden = true;
            if (_resume)
                component.Pause();
        }
    }
}
