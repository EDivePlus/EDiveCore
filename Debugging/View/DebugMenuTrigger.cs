// Author: František Holubec
// Created: 14.10.2026

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Invisible top left corner, hold to open. Shows error and warning counts below it.
    public sealed class DebugMenuTrigger : VisualElement
    {
        private readonly float _holdTime;
        private readonly VisualElement _area;
        private readonly VisualElement _fill;
        private readonly DebugLogCountBadge _badge;

        private IVisualElementScheduledItem _holdTick;
        private float _holdStart;
        private int _pointerId = -1;

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
            Add(_area);

            _badge = new DebugLogCountBadge(false);
            _badge.AddToClassList("dm-trigger__badge");
            _badge.RegisterCallback<ClickEvent>(_ => Activated?.Invoke());
            Add(_badge);

            _area.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _area.RegisterCallback<PointerUpEvent>(_ => CancelHold());
            _area.RegisterCallback<PointerCancelEvent>(_ => CancelHold());
            _area.RegisterCallback<PointerCaptureOutEvent>(_ => CancelHold());
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
