// Author: František Holubec
// Created: 08.10.2026

using System;
using System.Collections.Generic;

namespace EDIVE.View.ViewTree
{
    public class ViewStateRequests
    {
        private readonly Dictionary<object, ViewState> _requests = new();
        private readonly Action<ViewState> _stateChanged;
        private bool _isApplied;

        public ViewState State { get; private set; } = ViewState.Focused;
        public IReadOnlyDictionary<object, ViewState> Requests => _requests;

        public ViewStateRequests(Action<ViewState> stateChanged)
        {
            _stateChanged = stateChanged;
        }

        public ViewStateRequests(AViewNode node) : this(node.SetState)
        {
        }

        public void Set(object requester, ViewState cap)
        {
            _requests[requester] = cap;
            Recalculate();
        }

        public void Remove(object requester)
        {
            _requests.Remove(requester);
            Recalculate();
        }

        public void Clear()
        {
            _requests.Clear();
            Recalculate();
        }

        private void Recalculate()
        {
            var state = ViewState.Focused;
            foreach (var cap in _requests.Values)
            {
                if (cap < state)
                    state = cap;
            }

            if (_isApplied && state == State)
                return;

            State = state;
            _isApplied = true;
            _stateChanged?.Invoke(state);
        }
    }
}
