// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;

namespace EDIVE.Debugging
{
    // Handle to a section, returned by Section(). Dispose and Clear remove only controls added through this handle.
    public sealed partial class DebugSection : IDisposable
    {
        private readonly List<ADebugControl> _controls = new();

        internal DebugScope Scope { get; }
        internal DebugSectionNode Node { get; }

        public DebugPage Page => Node.Page;
        public string Name => Node.Name;
        public IReadOnlyList<ADebugControl> Controls => _controls;

        internal DebugSection(DebugScope scope, DebugSectionNode node)
        {
            Scope = scope;
            Node = node;
        }

        public DebugSection Order(int order)
        {
            Node.SortOrder = order;
            DebugMenu.NotifyChanged();
            return this;
        }

        public DebugSection Collapsed(bool collapsed = true)
        {
            Node.Collapsed = collapsed;
            DebugMenu.NotifyChanged();
            return this;
        }

        // Adds a custom control.
        public T Add<T>(T control) where T : ADebugControl
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (control.Handle != null)
                throw new InvalidOperationException($"Debug control '{control.Label}' is already registered.");

            control.Handle = this;
            control.Sequence = DebugMenu.NextSequence();
            _controls.Add(control);
            Node.Controls.Add(control);
            Scope?.Track(control);
            DebugMenu.NotifyChanged();
            return control;
        }

        public void Refresh()
        {
            foreach (var control in _controls)
            {
                control.Refresh();
            }
        }

        public void Clear()
        {
            if (_controls.Count == 0)
                return;

            foreach (var control in _controls)
            {
                Node.Controls.Remove(control);
                Scope?.Untrack(control);
                control.Handle = null;
            }
            _controls.Clear();
            DebugMenu.NotifyChanged();
        }

        public void Dispose() => Clear();

        internal void Remove(ADebugControl control)
        {
            if (!_controls.Remove(control))
                return;

            Node.Controls.Remove(control);
            Scope?.Untrack(control);
            control.Handle = null;
            DebugMenu.NotifyChanged();
        }
    }
}
