// Author: František Holubec
// Created: 08.10.2026

using System;
using EDIVE.Conditions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.ViewTree.Conditions
{
    [Serializable]
    public class ViewStateCondition : AValueComparisonCondition<ViewState>
    {
        public enum NodeSource
        {
            Node,
            // Group in a variable, no group counts as hidden
            Variable
        }

        [SerializeField]
        private NodeSource _Source;

        [SerializeField]
        [ShowIf(nameof(_Source), NodeSource.Node)]
        private AViewNode _Node;

        [SerializeField]
        [Required]
        [ShowIf(nameof(_Source), NodeSource.Variable)]
        private ViewGroupScriptableVariable _Variable;

        private AViewNode _observed;

        public ViewStateCondition() : base(ViewState.Visible) { }

        protected override bool TryGetValue(out ViewState value)
        {
            var node = GetNode();
            value = node != null ? node.StateInTree : ViewState.Hidden;
            return _Source == NodeSource.Variable ? _Variable != null : node != null;
        }

        public override void InitializeObserving()
        {
            base.InitializeObserving();
            if (_Source == NodeSource.Variable && _Variable != null)
                _Variable.ValueChanged += OnVariableChanged;
            Observe(GetNode());
        }

        public override void TerminateObserving()
        {
            base.TerminateObserving();
            if (_Variable != null)
                _Variable.ValueChanged -= OnVariableChanged;
            Observe(null);
        }

        private AViewNode GetNode()
        {
            if (_Source == NodeSource.Node)
                return _Node;
            return _Variable != null ? _Variable.Value : null;
        }

        private void Observe(AViewNode node)
        {
            if (ReferenceEquals(_observed, node))
                return;

            if (!ReferenceEquals(_observed, null))
                _observed.StateChanged -= OnStateChanged;
            _observed = node;
            if (node != null)
                node.StateChanged += OnStateChanged;
        }

        private void OnVariableChanged()
        {
            Observe(GetNode());
            InvokeStateChanged();
        }

        private void OnStateChanged(ViewState from, ViewState to) => InvokeStateChanged();
    }
}
