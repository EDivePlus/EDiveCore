// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using EDIVE.Conditions;
using UnityEngine;

#if R3
using R3;
#endif

namespace EDIVE.Debugging
{
    // One row in a section. Getters are read when the row is shown and on Refresh, chain Every/RefreshOn for more.
    public abstract class ADebugControl
    {
        private readonly List<DebugControlView> _views = new();
        private readonly List<Func<Action, IDisposable>> _refreshTriggers = new();

        public string Label { get; }
        public int SortOrder { get; private protected set; }
        public bool IsRegistered => Handle != null;

        internal DebugSection Handle { get; set; }
        internal int Sequence { get; set; }

        // Seconds between refreshes while shown, 0 every frame, negative only on show and Refresh.
        internal float RefreshInterval { get; private protected set; } = -1f;
        internal Func<bool> EnabledFunc { get; private protected set; }
        internal Func<bool> VisibleFunc { get; private protected set; }
        internal ICondition VisibleCondition { get; private protected set; }
        internal IReadOnlyList<Func<Action, IDisposable>> RefreshTriggers => _refreshTriggers;
        internal bool IsOwnerDestroyed => Handle?.Scope?.IsOwnerDestroyed ?? false;

        // Text matched by search and used in the favourite key
        internal virtual string SearchText => Label;
        internal string Key => Handle == null ? null : $"{Handle.Page.Path}{DebugMenu.PAGE_SEPARATOR}{Handle.Name}{DebugMenu.PAGE_SEPARATOR}{SearchText}";

        protected ADebugControl(string label)
        {
            Label = label ?? string.Empty;
        }

        public void Refresh()
        {
            for (var i = _views.Count - 1; i >= 0; i--)
            {
                _views[i].Refresh();
            }
        }

        public void Remove() => Handle?.Remove(this);

        public abstract DebugControlView CreateView();

        internal void AddView(DebugControlView view) => _views.Add(view);
        internal void RemoveView(DebugControlView view) => _views.Remove(view);

        private protected void AddRefreshTrigger(Func<Action, IDisposable> trigger) => _refreshTriggers.Add(trigger);
    }

    // Chainable options shared by all controls.
    public abstract class ADebugControl<TSelf> : ADebugControl where TSelf : ADebugControl<TSelf>
    {
        protected TSelf Self => (TSelf) this;

        protected ADebugControl(string label) : base(label) { }

        public TSelf Order(int order)
        {
            SortOrder = order;
            if (IsRegistered) DebugMenu.NotifyChanged();
            return Self;
        }

        public TSelf Enabled(Func<bool> enabled)
        {
            EnabledFunc = enabled;
            return Self;
        }

        public TSelf Visible(Func<bool> visible)
        {
            VisibleFunc = visible;
            return Self;
        }

        public TSelf Visible(ICondition condition)
        {
            VisibleCondition = condition;
            if (condition != null)
                RefreshOn(a => condition.StateChanged += a, a => condition.StateChanged -= a);
            return Self;
        }

        // Re-read every few seconds while shown.
        public TSelf Every(float seconds)
        {
            RefreshInterval = Mathf.Max(0f, seconds);
            return Self;
        }

        // Re-read every frame while shown.
        public TSelf WhileVisible()
        {
            RefreshInterval = 0f;
            return Self;
        }

        // Re-read when a C# event fires: RefreshOn(a => x.Changed += a, a => x.Changed -= a).
        public TSelf RefreshOn(Action<Action> subscribe, Action<Action> unsubscribe)
        {
            AddRefreshTrigger(refresh =>
            {
                subscribe(refresh);
                return new ActionDisposable(() => unsubscribe(refresh));
            });
            return Self;
        }

#if R3
        // Re-read when the observable emits.
        public TSelf RefreshOn<T>(Observable<T> source)
        {
            AddRefreshTrigger(refresh => source.Subscribe(_ => refresh()));
            return Self;
        }
#endif
    }

    internal sealed class ActionDisposable : IDisposable
    {
        private Action _action;

        public ActionDisposable(Action action) => _action = action;

        public void Dispose()
        {
            _action?.Invoke();
            _action = null;
        }
    }
}
