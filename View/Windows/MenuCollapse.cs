// Author: František Holubec
// Created: 14.10.2026

using System;
using EDIVE.DataStructures;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using EDIVE.View.ViewTree;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.Windows
{
    // Collapses a menu to a small pill where room is short, e.g. phones with touch controls.
    // Collapsed hides the content, its windows keep running. Elsewhere the menu stays expanded.
    public class MenuCollapse : MonoBehaviour
    {
        [SerializeField]
        private PlatformSpecificValue<bool> _Collapsible = new();

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

        // Same as the overlay, e.g. touch controls hide
        [SerializeField]
        [EnhancedBoxGroup("Display")]
        private AScriptableVariable<bool> _OverlayVariable;

        private IDisposable _expandListener;
        private IDisposable _collapseListener;

        public bool IsCollapsible { get; private set; }
        public bool IsExpanded { get; private set; } = true;

        public event Action<bool> ExpandedChanged;

        private void Awake()
        {
            IsCollapsible = _Collapsible.GetCurrentPlatformValue();
            IsExpanded = !IsCollapsible || !_StartCollapsed;
        }

        private void OnEnable()
        {
            _expandListener = _ExpandActivation.RegisterDisposable(Expand);
            _collapseListener = _CollapseActivation.RegisterDisposable(Collapse);
            Apply(true);
        }

        private void OnDisable()
        {
            _expandListener?.Dispose();
            _expandListener = null;
            _collapseListener?.Dispose();
            _collapseListener = null;
            if (_OverlayVariable != null)
                _OverlayVariable.SetValue(false);
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
            Apply(false);
            ExpandedChanged?.Invoke(expanded);
        }

        private void Apply(bool immediate)
        {
            var overlay = IsCollapsible && IsExpanded;
            _Content.SetState(IsExpanded ? _Content.DefaultState : ViewState.Hidden);
            if (_ExpandedState != null)
                _ExpandedState.SetState(IsExpanded, immediate);
            if (_CollapsibleState != null)
                _CollapsibleState.SetState(IsCollapsible, immediate);
            if (_OverlayState != null)
                _OverlayState.SetState(overlay, immediate);
            if (_OverlayVariable != null)
                _OverlayVariable.SetValue(overlay);
        }
    }
}
