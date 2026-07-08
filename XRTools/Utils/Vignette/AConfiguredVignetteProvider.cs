// Author: František Holubec
// Created: 08.07.2026

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.XRTools.Utils.Vignette
{
    [Serializable]
    public abstract class AConfiguredVignetteProvider : AVignetteProvider
    {
        [SerializeField]
        private bool _OverrideSettings;

        [SerializeField]
        [ShowIf(nameof(_OverrideSettings))]
        [InlineProperty, HideLabel, FoldoutGroup("Settings")]
        private VignetteSettings _Settings = VignetteSettings.Default;

        [SerializeField]
        private bool _OverrideTransition;

        [SerializeField]
        [ShowIf(nameof(_OverrideTransition))]
        [InlineProperty, HideLabel, FoldoutGroup("Transition")]
        private VignetteTransition _Transition = VignetteTransition.Default;

        // Shared with the handle, edits apply live
        public VignetteSettings Settings => _Settings;

        protected override VignetteSettings GetRequestSettings() => _OverrideSettings ? _Settings : null;
        protected override VignetteTransition? GetRequestTransition() => _OverrideTransition ? _Transition : null;
    }
}
