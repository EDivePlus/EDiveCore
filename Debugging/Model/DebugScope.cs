// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;

namespace EDIVE.Debugging
{
    // Everything registered by one owner. Dispose removes only this owner's controls.
    public sealed class DebugScope : IDisposable
    {
        private readonly List<ADebugControl> _controls = new();
        private readonly UnityEngine.Object _unityOwner;
        private readonly bool _hasUnityOwner;

        public object Owner { get; }

        internal bool IsOwnerDestroyed => _hasUnityOwner && _unityOwner == null;

        internal DebugScope(object owner)
        {
            Owner = owner;
            _unityOwner = owner as UnityEngine.Object;
            _hasUnityOwner = _unityOwner is not null;
        }

        // Section handle on a page path, the section name is optional.
        public DebugSection Section(string page, string section = null)
        {
            var node = DebugMenu.Page(page).GetOrCreateSection(section);
            return new DebugSection(this, node);
        }

        internal void Track(ADebugControl control) => _controls.Add(control);

        internal void Untrack(ADebugControl control) => _controls.Remove(control);

        public void Dispose()
        {
            if (_controls.Count > 0)
            {
                foreach (var control in _controls.ToArray())
                {
                    control.Remove();
                }
                _controls.Clear();
            }

            DebugMenu.RemoveScope(this);
        }
    }
}
