// Author: František Holubec
// Created: 14.10.2026

using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EDIVE.Tweening;
using EDIVE.Conditions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using EDIVE.View.ViewTree;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.Windows
{
    // Collapses a screen space menu to a small pill, e.g. desktop or phone. A world space menu, e.g. the VR tablet, stays expanded.
    // Collapsed hides the content, its windows keep running.
    public class MenuCollapse : MonoBehaviour
    {
        // Hidden while collapsed, e.g. the screen root
        [SerializeField]
        [Required]
        private ViewGroup _Content;

        [SerializeField]
        private bool _StartCollapsed;

        [SerializeReference]
        private IActivation _ExpandActivation;

        [SerializeReference]
        private IActivation _CollapseActivation;

        // On while expanded, e.g. the dock instead of the pill
        [SerializeField]
        [EnhancedBoxGroup("Display")]
        private AToggleState _ExpandedState;

        // On where it can collapse, e.g. the collapse button
        [SerializeField]
        [EnhancedBoxGroup("Display")]
        private AToggleState _CollapsibleState;

        // On while collapsible and expanded, e.g. a scrim over the scene
        [SerializeField]
        [EnhancedBoxGroup("Display")]
        private AToggleState _OverlayState;

        // Overlay shown only while true, e.g. touch screens fold the menu on a tap outside, desktop keeps it open. Empty is always.
        [SerializeReference]
        [EnhancedBoxGroup("Display")]
        private ICondition _OverlayCondition;

        // Same as the overlay, e.g. touch controls hide
        [SerializeField]
        [EnhancedBoxGroup("Display")]
        private AScriptableVariable<bool> _OverlayVariable;

        // Before collapsing, e.g. windows and dock fade out
        [SerializeField]
        private TweenAnimationField _CollapseAnimation = new();

        // After collapsing, e.g. the pill comes in
        [SerializeField]
        private TweenAnimationField _CollapsedAnimation = new();

        // After expanding, e.g. dock and windows come back
        [SerializeField]
        private TweenAnimationField _ExpandAnimation = new();

        private int _transition;
        private IDisposable _expandListener;
        private IDisposable _collapseListener;

        public bool IsCollapsible { get; private set; }
        public bool IsExpanded { get; private set; } = true;

        public event Action<bool> ExpandedChanged;

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>(true);
            IsCollapsible = canvas != null && canvas.rootCanvas.renderMode != RenderMode.WorldSpace;
            IsExpanded = !IsCollapsible || !_StartCollapsed;
        }

        private void OnEnable()
        {
            _expandListener = _ExpandActivation.RegisterDisposable(Expand);
            _collapseListener = _CollapseActivation.RegisterDisposable(Collapse);
            if (_OverlayCondition != null)
            {
                _OverlayCondition.InitializeObserving();
                _OverlayCondition.StateChanged += OnOverlayConditionChanged;
            }
            Apply(true);
        }

        private void OnDisable()
        {
            _expandListener?.Dispose();
            _expandListener = null;
            _collapseListener?.Dispose();
            _collapseListener = null;
            if (_OverlayCondition != null)
            {
                _OverlayCondition.StateChanged -= OnOverlayConditionChanged;
                _OverlayCondition.TerminateObserving();
            }
            if (_OverlayVariable != null)
                _OverlayVariable.SetValue(false);
        }

        private void OnDestroy()
        {
            _CollapseAnimation.Kill();
            _CollapsedAnimation.Kill();
            _ExpandAnimation.Kill();
        }

        public void Expand() => SetExpanded(true);
        public void Collapse() => SetExpanded(false);

        public void SetExpanded(bool expanded)
        {
            if (!IsCollapsible)
                expanded = true;
            if (IsExpanded == expanded)
                return;

            IsExpanded = expanded;
            ExpandedChanged?.Invoke(expanded);
            TransitionAsync(expanded).Forget();
        }

        // Collapse plays out first and swaps after, expand swaps first and plays in after
        private async UniTaskVoid TransitionAsync(bool expanded)
        {
            var transition = ++_transition;
            _CollapseAnimation.Kill();
            _CollapsedAnimation.Kill();
            _ExpandAnimation.Kill();

            if (expanded)
            {
                Apply(false);
                _ = _ExpandAnimation.Play();
                return;
            }

            _ = _CollapseAnimation.Play();
            var tween = _CollapseAnimation.Tween;
            if (tween.IsActive())
                await tween.ToUniTask(cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
            if (transition != _transition)
                return;
            Apply(false);
            _ = _CollapsedAnimation.Play();
        }

        private void OnOverlayConditionChanged() => Apply(false);

        private void Apply(bool immediate)
        {
            var overlay = IsCollapsible && IsExpanded;
            _Content.SetState(IsExpanded ? _Content.DefaultState : ViewState.Hidden);
            if (_ExpandedState != null)
                _ExpandedState.SetState(IsExpanded, immediate);
            if (_CollapsibleState != null)
                _CollapsibleState.SetState(IsCollapsible, immediate);
            if (_OverlayState != null)
                _OverlayState.SetState(overlay && (_OverlayCondition == null || _OverlayCondition.Evaluate()), immediate);
            if (_OverlayVariable != null)
                _OverlayVariable.SetValue(overlay);
        }
    }
}
