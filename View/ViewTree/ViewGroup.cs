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

        [Tooltip("Optional, holds this group so others can read it without a reference")]
        [SerializeField]
        [ShowCreateNew]
        [EnhancedBoxGroup("View")]
        private ViewGroupScriptableVariable _Variable;

        public override ViewState DefaultState => _DefaultState;

        [ShowInInspector]
        [ReadOnly]
        [HideInEditorMode]
        [EnhancedBoxGroup("View")]
        private readonly List<AViewNode> _children = new();

        public IReadOnlyList<AViewNode> Children => _children;

        private bool _subtreeLinked;
        private int _linkedChildCount;

        protected override void Awake()
        {
            base.Awake();
            if (_Variable != null)
                _Variable.Value = this;
            LinkSubtree();
        }

        // Content created inactive under a group that already linked its subtree
        public static void LinkHierarchy(Transform root)
        {
            if (root != null)
                LinkBranch(root);
        }

        // Nodes below that never woke, e.g. hidden pages, join the tree now
        private void LinkSubtree()
        {
            if (_subtreeLinked)
                return;

            _subtreeLinked = true;
            _linkedChildCount = transform.childCount;
            for (var i = 0; i < transform.childCount; i++)
                LinkBranch(transform.GetChild(i));
        }

        // Only direct children notify, deeper content needs LinkHierarchy
        private void OnTransformChildrenChanged()
        {
            // Not linked yet, Awake links everything
            if (!_subtreeLinked)
                return;

            var count = transform.childCount;
            var added = count > _linkedChildCount;
            _linkedChildCount = count;

            // Added children always come last
            if (added)
                LinkBranch(transform.GetChild(count - 1));
        }

        // Stops at groups that already linked theirs
        private static void LinkBranch(Transform target)
        {
            var nodes = ListPool<AViewNode>.Get();
            target.GetComponents(nodes);
            var descend = true;
            foreach (var node in nodes)
            {
                if (!node.IsAttached)
                    node.Reattach();

                if (node is ViewGroup group)
                {
                    if (group._subtreeLinked)
                        descend = false;
                    group._subtreeLinked = true;
                    group._linkedChildCount = target.childCount;
                }
            }
            ListPool<AViewNode>.Release(nodes);

            if (!descend)
                return;

            for (var i = 0; i < target.childCount; i++)
                LinkBranch(target.GetChild(i));
        }

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

            if (_Variable != null && _Variable.Value == this)
                _Variable.Value = null;

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

        internal ViewState GetStateForChild(AViewNode child)
        {
            var cap = GetChildCap(child);
            return cap < StateInTree ? cap : StateInTree;
        }

        // Extra cap for one child, e.g. only the selected page of a switch is shown
        protected virtual ViewState GetChildCap(AViewNode child) => ViewState.Focused;

        // Call when GetChildCap results change
        protected void RefreshChildCaps() => RefreshChildren();

        private protected override void RefreshChildren()
        {
            // Nodes destroyed before Awake never detach
            for (var i = _children.Count - 1; i >= 0; i--)
            {
                if (_children[i] == null)
                    _children.RemoveAt(i);
            }

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

        private protected override void ResetChildren()
        {
            var children = ListPool<AViewNode>.Get();
            children.AddRange(_children);
            foreach (var child in children)
            {
                if (child != null && child.Parent == this)
                    child.ResetView();
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
