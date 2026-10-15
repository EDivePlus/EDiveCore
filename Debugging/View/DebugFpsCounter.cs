// Author: František Holubec
// Created: 15.10.2026

using System;
using System.Globalization;
using EDIVE.External.DomainReloadHelper;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // FPS in the top right corner while the menu is closed. Toggled from the Profiler, saved per device.
    public sealed class DebugFpsCounter : VisualElement
    {
        private const string PREFS_KEY = "EDIVE.DebugMenu.ShowFps";
        private const long INTERVAL_MS = 250;

        [ClearOnReload]
        private static bool? _enabled;

        [ClearOnReload]
        public static event Action EnabledChanged;

        private readonly Label _value;
        private IVisualElementScheduledItem _tick;
        private bool _menuOpen;
        private int _lastFrame;
        private float _lastTime;

        public static bool Enabled
        {
            get => _enabled ??= PlayerPrefs.GetInt(PREFS_KEY, 0) == 1;
            set
            {
                if (Enabled == value)
                    return;
                _enabled = value;
                PlayerPrefs.SetInt(PREFS_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
                EnabledChanged?.Invoke();
            }
        }

        // Display refresh rate, capped by the target frame rate
        internal static float TargetFps
        {
            get
            {
                var rate = (float) Screen.currentResolution.refreshRateRatio.value;
                if (Application.targetFrameRate > 0)
                    rate = Mathf.Min(rate > 0f ? rate : float.MaxValue, Application.targetFrameRate);
                return rate > 0f ? rate : 60f;
            }
        }

        public DebugFpsCounter()
        {
            AddToClassList("dm-fps");
            pickingMode = PickingMode.Ignore;
            _value = new Label {pickingMode = PickingMode.Ignore};
            _value.AddToClassList("dm-fps__value");
            Add(_value);
            var unit = new Label("FPS") {pickingMode = PickingMode.Ignore};
            unit.AddToClassList("dm-fps__unit");
            Add(unit);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                EnabledChanged += UpdateVisibility;
                UpdateVisibility();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                EnabledChanged -= UpdateVisibility;
                _tick?.Pause();
            });
        }

        public void SetMenuOpen(bool open)
        {
            _menuOpen = open;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var shown = Enabled && !_menuOpen;
            style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown)
            {
                _tick?.Pause();
                return;
            }

            _lastFrame = Time.frameCount;
            _lastTime = Time.unscaledTime;
            _value.text = "-";
            _tick ??= schedule.Execute(UpdateValue).Every(INTERVAL_MS);
            _tick.Resume();
        }

        private void UpdateValue()
        {
            var time = Time.unscaledTime;
            var elapsed = time - _lastTime;
            if (elapsed <= 0f)
                return;

            var fps = (Time.frameCount - _lastFrame) / elapsed;
            _lastFrame = Time.frameCount;
            _lastTime = time;
            _value.text = fps.ToString("0", CultureInfo.InvariantCulture);

            var ratio = fps / TargetFps;
            EnableInClassList("dm-fps--warning", ratio is < 0.9f and >= 0.6f);
            EnableInClassList("dm-fps--error", ratio < 0.6f);
        }
    }
}
