// Author: František Holubec
// Created: 08.07.2026

using System;
using DG.Tweening;
using UnityEngine;

namespace EDIVE.XRTools.Utils.Vignette
{
    [Serializable]
    public struct VignetteTransition
    {
        [SerializeField, Min(0f)]
        private float _ShowDuration;

        [SerializeField]
        private Ease _ShowEase;

        [SerializeField, Min(0f)]
        private float _HideDuration;

        [SerializeField]
        private Ease _HideEase;

        [SerializeField]
        [Tooltip("When a source is released mid-show, finish easing in before easing out.")]
        private bool _CompleteShowBeforeHide;

        [SerializeField, Min(0f)]
        [Tooltip("Hold the shown vignette this long before easing out.")]
        private float _HideDelay;

        public float ShowDuration { get => _ShowDuration; set => _ShowDuration = value; }
        public Ease ShowEase { get => _ShowEase; set => _ShowEase = value; }
        public float HideDuration { get => _HideDuration; set => _HideDuration = value; }
        public Ease HideEase { get => _HideEase; set => _HideEase = value; }
        public bool CompleteShowBeforeHide { get => _CompleteShowBeforeHide; set => _CompleteShowBeforeHide = value; }
        public float HideDelay { get => _HideDelay; set => _HideDelay = value; }

        public static VignetteTransition Default => new VignetteTransition
        {
            ShowDuration = 0.3f,
            ShowEase = Ease.OutCubic,
            HideDuration = 0.3f,
            HideEase = Ease.InCubic,
            CompleteShowBeforeHide = false,
            HideDelay = 0f,
        };

        public static VignetteTransition Instant => new()
        {
            ShowEase = Ease.Linear,
            HideEase = Ease.Linear,
        };
    }
    
    public enum VignetteTiebreak
    {
        [Tooltip("Smallest aperture")] StrongestEffect,
        MostRecent,
        Oldest,
    }
}
