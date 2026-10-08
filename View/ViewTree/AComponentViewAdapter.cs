// Author: František Holubec
// Created: 08.10.2026

using System;
using UnityEngine;

namespace EDIVE.View.ViewTree
{
    public interface IComponentViewAdapter
    {
        Component Target { get; }
        Type TargetType { get; }
    }

    // Drives one component on the same object
    public abstract class AComponentViewAdapter<TComponent> : AViewBehaviour, IComponentViewAdapter where TComponent : Component
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

        protected virtual void Reset() => _Target = ViewAdapterUtility.FindUnhandledTarget(gameObject, typeof(TComponent), this) as TComponent;
    }

    public static class ViewAdapterUtility
    {
        // Some adapter on the same object targets it
        public static bool IsHandled(Component component, IComponentViewAdapter except = null)
        {
            foreach (var adapter in component.GetComponents<IComponentViewAdapter>())
            {
                if (!ReferenceEquals(adapter, except) && adapter.Target == component)
                    return true;
            }
            return false;
        }

        // First component of the type on the object no other adapter targets
        public static Component FindUnhandledTarget(GameObject gameObject, Type type, IComponentViewAdapter except = null)
        {
            foreach (var candidate in gameObject.GetComponents(type))
            {
                if (!IsHandled(candidate, except))
                    return candidate;
            }
            return null;
        }
    }
}
