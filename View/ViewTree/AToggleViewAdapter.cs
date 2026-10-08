// Author: František Holubec
// Created: 08.10.2026

using System;
using UnityEngine;

namespace EDIVE.View.ViewTree
{
    // Switches something on while the node is at least the given state
    public abstract class AToggleViewAdapter : AViewBehaviour
    {
        [SerializeField]
        private ViewState _ActiveWhile = ViewState.Visible;

        public ViewState ActiveWhile
        {
            get => _ActiveWhile;
            set
            {
                _ActiveWhile = value;
                RefreshHandlers();
            }
        }

        protected virtual ViewState DefaultActiveWhile => ViewState.Visible;

        protected override void ApplyState(ViewState state) => SetActive(state >= _ActiveWhile);

        protected abstract void SetActive(bool active);

        protected virtual void Reset() => _ActiveWhile = DefaultActiveWhile;
    }

    // Switches its target component on the same object
    public abstract class AComponentToggleViewAdapter<TComponent> : AToggleViewAdapter, IComponentViewAdapter where TComponent : Component
    {
        [SerializeField]
        private TComponent _Target;

        public TComponent Target => _Target;

        Component IComponentViewAdapter.Target => _Target;
        public Type TargetType => typeof(TComponent);

        // For adapters added at runtime, Reset only fills the target in the editor
        public void Initialize(TComponent target)
        {
            _Target = target;
            RefreshHandlers();
        }

        protected override void SetActive(bool active)
        {
            if (_Target != null)
                SetActive(_Target, active);
        }

        protected abstract void SetActive(TComponent component, bool active);

        protected override void Reset()
        {
            base.Reset();
            _Target = ViewAdapterUtility.FindUnhandledTarget(gameObject, typeof(TComponent), this) as TComponent;
        }
    }
}
