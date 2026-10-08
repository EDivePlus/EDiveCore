// Author: František Holubec
// Created: 08.10.2026

using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace EDIVE.View.ViewTree
{
    [DisallowMultipleComponent]
    public class ViewGroup : AViewNode
    {
        [SerializeField]
        [HideInPlayMode]
        [PropertyOrder(-1)]
        [EnhancedBoxGroup("View")]
        [FormerlySerializedAs("_State")]
        private ViewState _DefaultState = ViewState.Focused;

        public override ViewState DefaultState => _DefaultState;

        [ShowInInspector]
        [ReadOnly]
        [HideInEditorMode]
        [EnhancedBoxGroup("View")]
        private readonly List<AViewNode> _children = new();

        public IReadOnlyList<AViewNode> Children => _children;

        protected override void OnDestroy()
        {
            var children = ListPool<AViewNode>.Get();
            children.AddRange(_children);
            _children.Clear();

            // Children still enabled means only this component goes, Auto ones move up
            var fallback = FindHierarchyParent();
            foreach (var child in children)
            {
                if (child == null || child.Parent != this)
                    continue;

                if (child.AttachMode != AttachMode.Auto || !child.isActiveAndEnabled)
                    child.SetParent(null, true);
                else if (fallback != null)
                    fallback.Attach(child);
                else
                    child.SetParent(null);
            }
            ListPool<AViewNode>.Release(children);

            base.OnDestroy();
        }

        public override ViewGroup FindHierarchyParent()
        {
            var parent = transform.parent;
            return parent != null ? parent.GetComponentInParent<ViewGroup>(true) : null;
        }

        public void Attach(AViewNode child)
        {
            if (child == null || child.Parent == this)
                return;

            if (IsSelfOrAncestor(child))
            {
                Debug.LogError($"[ViewGroup] Can't attach '{child.name}' to '{name}', it would create a cycle.", this);
                return;
            }

            if (child.Parent != null)
                child.Parent.RemoveChild(child);

            _children.Add(child);
            child.SetParent(this);
        }

        public void Detach(AViewNode child)
        {
            if (child == null || child.Parent != this)
                return;

            RemoveChild(child);
            child.SetParent(null);
        }

        internal void RemoveChild(AViewNode child) => _children.Remove(child);

        private protected override void RefreshChildren()
        {
            var children = ListPool<AViewNode>.Get();
            children.AddRange(_children);
            foreach (var child in children)
            {
                if (child == null || child.Parent != this)
                    continue;

                try
                {
                    child.Refresh();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, child);
                }
            }
            ListPool<AViewNode>.Release(children);
        }

        private bool IsSelfOrAncestor(AViewNode node)
        {
            for (AViewNode current = this; current != null; current = current.Parent)
            {
                if (current == node)
                    return true;
            }
            return false;
        }
    }
}
