// Author: František Holubec
// Created: 14.10.2026

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Invisible top left corner, hold to open. A dot shows logs that came since the menu was last open,
    // red for errors, amber for warnings only.
    public sealed class DebugMenuTrigger : VisualElement
    {
        private readonly float _holdTime;
        private readonly VisualElement _area;
        private readonly VisualElement _fill;
        private readonly VisualElement _dot;

        private IVisualElementScheduledItem _holdTick;
        private float _holdStart;
        private int _pointerId = -1;
        private bool _dotScheduled;
        private int _seenErrors;
        private int _seenWarnings;

        public event Action Activated;

        public DebugMenuTrigger(float holdTime)
        {
            _holdTime = Mathf.Max(0.1f, holdTime);
            AddToClassList("dm-trigger");
            pickingMode = PickingMode.Ignore;

            _area = new VisualElement();
            _area.AddToClassList("dm-trigger__area");
            _fill = new VisualElement {pickingMode = PickingMode.Ignore};
            _fill.AddToClassList("dm-trigger__fill");
            _area.Add(_fill);
            _dot = new VisualElement {pickingMode = PickingMode.Ignore};
            _dot.AddToClassList("dm-trigger__dot");
            _area.Add(_dot);
            Add(_area);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                DebugLogBuffer.Changed += ScheduleDot;
                UpdateDot();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => DebugLogBuffer.Changed -= ScheduleDot);

            _area.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _area.RegisterCallback<PointerUpEvent>(_ => CancelHold());
            _area.RegisterCallback<PointerCancelEvent>(_ => CancelHold());
            _area.RegisterCallback<PointerCaptureOutEvent>(_ => CancelHold());
        }

        // Panel units, corner radius follows
        public void SetSize(float size)
        {
            _area.style.width = size;
            _area.style.height = size;
            var radius = size * 0.25f;
            _area.style.borderBottomRightRadius = radius;
            _fill.style.borderBottomRightRadius = radius;
        }

        private void ScheduleDot()
        {
            if (_dotScheduled)
                return;
            _dotScheduled = true;
            schedule.Execute(() =>
            {
                _dotScheduled = false;
                UpdateDot();
            });
        }

        // Called when the menu opens or closes, everything logged so far counts as seen
        public void MarkSeen()
        {
            _seenErrors = DebugLogBuffer.Count(DebugLogLevel.Error);
            _seenWarnings = DebugLogBuffer.Count(DebugLogLevel.Warning);
            UpdateDot();
        }

        private void UpdateDot()
        {
            var errorCount = DebugLogBuffer.Count(DebugLogLevel.Error);
            var warningCount = DebugLogBuffer.Count(DebugLogLevel.Warning);
            // Cleared or trimmed buffer, counts went down
            _seenErrors = Mathf.Min(_seenErrors, errorCount);
            _seenWarnings = Mathf.Min(_seenWarnings, warningCount);

            var errors = errorCount > _seenErrors;
            var warnings = warningCount > _seenWarnings;
            _dot.EnableInClassList("dm-trigger__dot--error", errors);
            _dot.EnableInClassList("dm-trigger__dot--warning", !errors && warnings);
            _dot.style.display = errors || warnings ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_pointerId >= 0)
                return;

            _pointerId = evt.pointerId;
            _area.CapturePointer(evt.pointerId);
            _holdStart = Time.unscaledTime;
            _area.AddToClassList("dm-trigger__area--holding");
            _holdTick = schedule.Execute(UpdateHold).Every(16);
            evt.StopPropagation();
        }

        private void UpdateHold()
        {
            var progress = Mathf.Clamp01((Time.unscaledTime - _holdStart) / _holdTime);
            _fill.style.opacity = progress;
            if (progress < 1f)
                return;

            CancelHold();
            Activated?.Invoke();
        }

        private void CancelHold()
        {
            if (_pointerId < 0)
                return;

            if (_area.HasPointerCapture(_pointerId))
                _area.ReleasePointer(_pointerId);
            _pointerId = -1;
            _holdTick?.Pause();
            _holdTick = null;
            _fill.style.opacity = 0f;
            _area.RemoveFromClassList("dm-trigger__area--holding");
        }
    }
}
