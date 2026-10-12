// Author: František Holubec
// Created: 12.10.2026

using System;
using System.Collections.Generic;

namespace EDIVE.NativeUtils
{
    // Disposes everything added to it at once. Clear keeps it usable, e.g. OnEnable/OnDisable. Main thread only.
    public sealed class DisposableGroup : IDisposable
    {
        private List<IDisposable> _items = new();
        private List<IDisposable> _disposing = new();
        private bool _isDisposed;

        public int Count => _items.Count;

        // Once disposed, added items get disposed right away
        public void Add(IDisposable item)
        {
            if (item == null)
                return;

            if (_isDisposed)
            {
                item.Dispose();
                return;
            }

            _items.Add(item);
        }

        // Disposes all added items, the group stays usable
        public void Clear()
        {
            // Swapped first, items may add to the group while disposing
            (_items, _disposing) = (_disposing, _items);
            foreach (var item in _disposing)
            {
                item.Dispose();
            }
            _disposing.Clear();
        }

        public void Dispose()
        {
            _isDisposed = true;
            Clear();
        }
    }
}
