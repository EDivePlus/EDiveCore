// Author: František Holubec
// Created: 08.10.2026

using System;
using EDIVE.Conditions;
using UnityEngine;

namespace EDIVE.View.ViewTree.Conditions
{
    [Serializable]
    public class ViewStateCondition : AValueComparisonCondition<ViewState>
    {
        [SerializeField]
        private AViewNode _Node;

        public ViewStateCondition() : base(ViewState.Visible) { }

        protected override bool TryGetValue(out ViewState value)
        {
            value = _Node != null ? _Node.StateInTree : ViewState.Hidden;
            return _Node != null;
        }

        public override void InitializeObserving()
        {
            base.InitializeObserving();
            if (_Node != null)
                _Node.StateChanged += OnStateChanged;
        }

        public override void TerminateObserving()
        {
            base.TerminateObserving();
            if (_Node != null)
                _Node.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(ViewState from, ViewState to) => InvokeStateChanged();
    }
}
