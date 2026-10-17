using System;
using DG.Tweening;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.Tweening
{
    [Serializable]
    public abstract class ATweenAnimationPlayer : ITweenAnimationPlayer
    {
        private bool IsActive => _tweener != null && _tweener.IsActive(); // Check so we dont get DoTween warnings
        public bool IsPlaying => IsActive && _tweener.IsPlaying();
        public bool IsPaused => IsActive && _tweener.IsInitialized() && !_tweener.IsPlaying() && !_tweener.IsComplete();
        public bool IsInitialized => IsActive && _tweener.IsInitialized();

        protected Sequence _tweener;
        public Tween Tween => _tweener;

        // Quitting tears objects down and recreates DOTween, tweens started then hit destroyed targets
        private static bool _quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void TrackQuitting()
        {
            _quitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting() => _quitting = true;

        public abstract Sequence CreateSequence();

        public Tween Play()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return EditorPreview();
            }
#endif
            return PlayInternal();
        }

        public void Complete()
        {
            _tweener?.Complete(true);
        }

        public void Kill(bool complete = false)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                KillEditorPreview(complete);
                return;
            }
#endif
            KillInternal(complete);
        }

        private Tween PlayInternal()
        {
            Kill();
            if (_quitting)
                return null;
            _tweener = CreateSequence();
            _tweener.Play();
            return _tweener;
        }

        private void KillInternal(bool complete = false)
        {
            // Complete called manually to invoke callbacks!
            if (complete) _tweener?.Complete(true);
            _tweener?.Kill();
            _tweener = null;
        }

        public void Resume()
        {
            _tweener?.Play();
        }

        public void Pause()
        {
            _tweener?.Pause();
        }

        public void Rewind()
        {
            _tweener?.Rewind();
        }

        public void SetToEnd()
        {
            Play();
            Kill(true);
        }

#if UNITY_EDITOR
        private Tween EditorPreview()
        {
            Rewind();
            KillEditorPreview();
            var tweener = PlayInternal();
            DoTweenEditorPreview.PrepareTweenForPreview(_tweener);
            DoTweenEditorPreview.Start();
            return tweener;
        }

        private void KillEditorPreview(bool complete = false)
        {
            // Complete called manually to invoke callbacks!
            if (complete) _tweener?.Complete(true);
            _tweener?.Kill();
            DoTweenEditorPreview.RemoveTweenFromPreview(_tweener);
            _tweener = null;
        }
#endif
    }
}

