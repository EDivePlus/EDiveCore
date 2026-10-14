// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Row element of one control. Reads the control when attached and on refresh, stops when detached.
    public abstract class DebugControlView : VisualElement
    {
        public const string ROW_CLASS = "dm-row";
        public const string LABEL_CLASS = "dm-row__label";

        private readonly List<IDisposable> _subscriptions = new();
        private IVisualElementScheduledItem _scheduled;

        public ADebugControl Control { get; }

        protected DebugControlView(ADebugControl control)
        {
            Control = control;
            AddToClassList(ROW_CLASS);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public void Refresh()
        {
            if (Control.IsOwnerDestroyed)
                return;

            try
            {
                var visible = (Control.VisibleFunc?.Invoke() ?? true) && (Control.VisibleCondition?.Evaluate() ?? true);
                style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible)
                    return;

                SetEnabled(Control.EnabledFunc?.Invoke() ?? true);
                Pull();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // Reads values from the control into the UI.
        protected abstract void Pull();

        protected Label AddLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList(LABEL_CLASS);
            Add(label);
            return label;
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            Control.AddView(this);
            Refresh();

            if (Control.RefreshInterval >= 0f)
                _scheduled = schedule.Execute(Refresh).Every((long) (Control.RefreshInterval * 1000f));

            foreach (var trigger in Control.RefreshTriggers)
            {
                _subscriptions.Add(trigger(Refresh));
            }
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            Control.RemoveView(this);
            _scheduled?.Pause();
            _scheduled = null;

            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
        }
    }
}
