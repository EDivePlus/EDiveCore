// Author: František Holubec
// Created: 08.10.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.ViewTree
{
    public abstract class AViewNode : MonoBehaviour
    {
        private const int MAX_REFRESH_PASSES = 10;

        [SerializeField]
        [EnhancedBoxGroup("View", 1f, 0.8f, 0.3f, order: -1000)]
        private AttachMode _AttachMode = AttachMode.Auto;

        [SerializeField]
        [EnhancedBoxGroup("View")]
        [ShowIf(nameof(_AttachMode), AttachMode.Explicit)]
        private ViewGroup _ExplicitParent;

        // Reattaches right away at runtime
        public AttachMode AttachMode
        {
            get => _AttachMode;
            set
            {
                _AttachMode = value;
                if (_isAwake)
                    Reattach();
            }
        }

        // Reattaches right away at runtime when Explicit
        public ViewGroup ExplicitParent
        {
            get => _ExplicitParent;
            set
            {
                _ExplicitParent = value;
                if (_isAwake && _AttachMode == AttachMode.Explicit)
                    Reattach();
            }
        }

        // Own cap before any SetState, groups serialize it
        public virtual ViewState DefaultState => ViewState.Focused;

        // Own cap, the tree state never goes above it
        [ShowInInspector]
        [HideInEditorMode]
        [LabelText("State")]
        [PropertyOrder(-1)]
        [EnhancedBoxGroup("View")]
        public ViewState StateSelf
        {
            get => _stateSelf ?? DefaultState;
            set => SetState(value);
        }

        [ShowInInspector]
        [ReadOnly]
        [HideInEditorMode]
        [EnhancedBoxGroup("View")]
        public ViewState StateInTree => _state ?? ViewState.Hidden;

        public ViewGroup Parent { get; private set; }

        // Runtime parent, in edit mode the one Auto finds. Empty means root.
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Parent")]
        [ShowIf(nameof(ShowParentPreview))]
        [EnhancedBoxGroup("View")]
        private ViewGroup ParentPreview => Application.isPlaying ? Parent : FindHierarchyParent();

        private bool ShowParentPreview => Application.isPlaying || _AttachMode == AttachMode.Auto;

        public bool VisibleInTree => StateInTree >= ViewState.Visible;
        public bool FocusedInTree => StateInTree >= ViewState.Focused;
        public bool IsAttached => Parent != null;

        // False until the first state is resolved
        public bool IsResolved => _state.HasValue;

        // Args are from, to
        public event Action<ViewState, ViewState> StateChanging;
        public event Action<ViewState, ViewState> StateChanged;

        private ViewState? _stateSelf;
        private ViewState? _state;
        private ViewState? _appliedState;
        private bool _isEnabled;
        private bool _isAwake;
        private bool _isOrphaned;
        private bool _isRefreshing;
        private bool _refreshPending;

        protected virtual void Awake()
        {
            _isAwake = true;
            Reattach();
        }

        protected virtual void OnEnable()
        {
            _isEnabled = true;
            Refresh();
        }

        protected virtual void OnDisable()
        {
            _isEnabled = false;
            Refresh();
        }

        protected virtual void OnDestroy()
        {
            if (Parent != null)
                Parent.Detach(this);
        }

        // Inspector edits at runtime skip the setters
        protected virtual void OnValidate()
        {
            if (!Application.isPlaying || !_isAwake)
                return;

            Reattach();
            RefreshHandlers();
        }

        protected virtual void OnTransformParentChanged()
        {
            if (!_isAwake || _AttachMode != AttachMode.Auto)
                return;

            Reattach();
        }

        // Nearest ViewGroup in the transform hierarchy
        public virtual ViewGroup FindHierarchyParent() => GetComponentInParent<ViewGroup>(true);

        // Parent by attach mode, Manual nodes have none
        public ViewGroup GetDesignatedParent() => _AttachMode switch
        {
            AttachMode.Auto => FindHierarchyParent(),
            AttachMode.Explicit => _ExplicitParent,
            _ => null
        };

        // Attaches to the designated parent again, Manual nodes keep theirs
        public void Reattach()
        {
            if (_AttachMode != AttachMode.Manual)
                AttachTo(GetDesignatedParent());
        }

        public void SetState(ViewState state)
        {
            // Destroyed nodes, e.g. a request owner outliving them
            if (this == null || StateSelf == state)
                return;

            _stateSelf = state;
            Refresh();
        }

        // Applies the current state again, e.g. after adapter settings changed at runtime
        public void RefreshHandlers()
        {
            if (!_state.HasValue)
                return;

            _appliedState = null;
            ApplyHandlers(_state.Value);
        }

        public void SetExplicitParent(ViewGroup parent)
        {
            _AttachMode = AttachMode.Explicit;
            ExplicitParent = parent;
        }

        private void AttachTo(ViewGroup parent)
        {
            if (ReferenceEquals(parent, Parent))
                return;

            if (parent != null)
            {
                parent.Attach(this);
                return;
            }

            // Assigned but destroyed parent leaves the node orphaned
            if (Parent != null)
                Parent.RemoveChild(this);
            SetParent(null, !ReferenceEquals(parent, null));
        }

        // Orphaned nodes lost their parent to destroy and stay hidden until attached again
        internal void SetParent(ViewGroup parent, bool orphaned = false)
        {
            var previous = Parent;
            Parent = parent;
            _isOrphaned = parent == null && orphaned;

            if (previous != null)
                OnDetached(previous);
            if (parent != null)
                OnAttached(parent);

            Refresh();
        }

        // Changes made during a refresh run after it, in order
        internal void Refresh()
        {
            if (_isRefreshing)
            {
                _refreshPending = true;
                return;
            }

            _isRefreshing = true;
            try
            {
                for (var pass = 1; ; pass++)
                {
                    _refreshPending = false;
                    RefreshState();
                    if (!_refreshPending)
                        break;

                    if (pass >= MAX_REFRESH_PASSES)
                    {
                        Debug.LogError($"[ViewTree] '{name}' keeps changing its state from its own callbacks.", this);
                        break;
                    }
                }
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void RefreshState()
        {
            if (!_isEnabled && !IsResolved)
                return;
            if (!TryGetParentState(out var parentState))
                return;

            var stateSelf = StateSelf;
            var state = !_isEnabled ? ViewState.Hidden : stateSelf < parentState ? stateSelf : parentState;
            var previous = _state;
            if (previous == state)
            {
                // Catches up handlers skipped while inactive
                ApplyHandlers(state);
                return;
            }

            _state = state;

            // First resolve counts as coming from Hidden
            var from = previous ?? ViewState.Hidden;

            // Rising notifies top-down, falling bottom-up
            var rising = previous.HasValue ? state > previous.Value : state != ViewState.Hidden;
            if (rising)
                Notify(from, state);

            RefreshChildren();

            if (!rising)
                Notify(from, state);
        }

        private bool TryGetParentState(out ViewState state)
        {
            state = ViewState.Focused;
            if (ReferenceEquals(Parent, null))
            {
                if (_isOrphaned)
                    state = ViewState.Hidden;
                return true;
            }

            // Destroyed without OnDestroy, e.g. never awake
            if (Parent == null)
            {
                state = ViewState.Hidden;
                return true;
            }

            if (Parent.IsResolved)
            {
                state = Parent.StateInTree;
                return true;
            }

            // Active parent resolves in its own OnEnable, inactive one counts as hidden
            if (Parent.isActiveAndEnabled)
                return false;

            state = ViewState.Hidden;
            return true;
        }

        private protected virtual void RefreshChildren()
        {
        }

        private void Notify(ViewState from, ViewState to)
        {
            try
            {
                OnViewStateChanging(from, to);
                StateChanging?.Invoke(from, to);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }

            ApplyHandlers(to);

            try
            {
                switch (to)
                {
                    case ViewState.Hidden:
                        OnViewHidden();
                        break;
                    case ViewState.Visible:
                        OnViewVisible();
                        break;
                    case ViewState.Focused:
                        OnViewFocused();
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }

            try
            {
                OnViewStateChanged(from, to);
                StateChanged?.Invoke(from, to);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        // Inactive or disabled nodes leave their components alone until active again
        private void ApplyHandlers(ViewState state)
        {
            if (_appliedState == state || !isActiveAndEnabled)
                return;

            _appliedState = state;
            try
            {
                ApplyState(state);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        // Handlers apply the state to their components here
        protected virtual void ApplyState(ViewState state)
        {
        }

        protected virtual void OnViewStateChanging(ViewState from, ViewState to)
        {
        }

        protected virtual void OnViewHidden()
        {
        }

        protected virtual void OnViewVisible()
        {
        }

        protected virtual void OnViewFocused()
        {
        }

        protected virtual void OnViewStateChanged(ViewState from, ViewState to)
        {
        }

        protected virtual void OnAttached(ViewGroup parent)
        {
        }

        protected virtual void OnDetached(ViewGroup parent)
        {
        }
    }
}
