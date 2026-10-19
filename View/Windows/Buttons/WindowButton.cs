// Author: František Holubec
// Created: 14.10.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using EDIVE.View.ViewTree;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;
using UnityEngine.Serialization;
using EDIVE.View.Windows.Screens;

namespace EDIVE.View.Windows.Buttons
{
    // Opens its window in the nearest screen, brings it forward when it is behind or minimized.
    // Dock buttons minimize the front window instead. Shows the window visual and its running and front states,
    // preset receivers on the same object get the visual too, e.g. a tooltip.
    public class WindowButton : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("_Definition")]
        private WindowDefinition _Window;

        [SerializeReference]
        [FormerlySerializedAs("_Activation")]
        private IActivation _OpenActivation;

        [Tooltip("Dock behaviour, a second press puts the window away")]
        [SerializeField]
        private bool _MinimizeWhenFront;

        [Tooltip("Optional pages to select, e.g. \"audio\"")]
        [SerializeField]
        [FormerlySerializedAs("_Path")]
        private string _PagePath;

        [SerializeField]
        [EnhancedBoxGroup("Display")]
        private VisualSwitcher _Switcher = new();

        [Tooltip("On while the window is open, minimized or not")]
        [SerializeField]
        [EnhancedBoxGroup("Display")]
        [FormerlySerializedAs("_RunningState")]
        private AToggleState _OpenState;

        [Tooltip("On while the window is the shown one of its layer")]
        [SerializeField]
        [EnhancedBoxGroup("Display")]
        private AToggleState _FrontState;

        private ScreenRoot _screen;
        private IDisposable _listener;

        public WindowDefinition Definition => _Window;

        // For spawned buttons, the screen is the nearest one when not given
        public void Setup(WindowDefinition definition, ScreenRoot screen = null)
        {
            _Window = definition;
            if (screen != null)
                Bind(screen);
            ApplyVisual();
            Refresh();
        }

        private void Start()
        {
            if (_screen == null)
                Bind(this.FindInViewTree<ScreenRoot>());
            ApplyVisual();
        }

        private void OnEnable()
        {
            _listener = _OpenActivation.RegisterDisposable(OnActivated);
            Refresh();
        }

        private void OnDisable()
        {
            _listener?.Dispose();
            _listener = null;
        }

        private void OnDestroy() => Bind(null);

        private void Bind(ScreenRoot screen)
        {
            if (_screen != null)
                _screen.Changed -= OnScreenChanged;

            _screen = screen;
            if (_screen != null)
                _screen.Changed += OnScreenChanged;
            Refresh();
        }

        private void ApplyVisual()
        {
            if (_Window == null)
                return;

            _Switcher.Apply(_Window.Visual);
            foreach (var receiver in GetComponents<IVisualPresetReceiver>())
                receiver.SetPreset(_Window.Visual);
        }

        private void OnActivated()
        {
            if (_screen == null)
                Bind(this.FindInViewTree<ScreenRoot>());

            if (_screen == null || _Window == null)
            {
                Debug.LogWarning($"[WindowButton] {name} has no screen or window to open.", this);
                return;
            }

            var handle = _screen.FindHandle(_Window);
            if (handle == null)
                handle = _screen.Open(_Window);
            else if (_MinimizeWhenFront && _screen.IsFront(handle))
            {
                _screen.Minimize(handle);
                return;
            }
            else
                _screen.BringToFront(handle);

            if (!string.IsNullOrEmpty(_PagePath))
                handle.Navigate(_PagePath);
        }

        private void OnScreenChanged(ScreenRoot screen) => Refresh();

        private void Refresh()
        {
            var handle = _screen != null && _Window != null ? _screen.FindHandle(_Window) : null;
            if (_OpenState != null)
                _OpenState.SetState(handle != null);
            if (_FrontState != null)
                _FrontState.SetState(handle != null && _screen.IsFront(handle));
        }
    }
}
