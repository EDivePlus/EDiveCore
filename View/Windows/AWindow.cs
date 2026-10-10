// Author: František Holubec
// Created: 09.10.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Tweening;
using EDIVE.View.ViewTree;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.Windows
{
    // Window root, opened into a ScreenRoot. Screen and lifecycle cap its view state.
    [RequireComponent(typeof(Canvas))]
    public abstract class AWindow : ViewGroup
    {
        private const string SCREEN_REQUEST = "Screen";
        private const string LIFECYCLE_REQUEST = "Lifecycle";

        // Lets layout settle before the open animation
        [SerializeField]
        [EnhancedBoxGroup("Window")]
        private int _OpenFramesDelay = 1;

        [SerializeField]
        [EnhancedBoxGroup("Window")]
        private TweenAnimationField _OpenAnimation;

        [SerializeField]
        [EnhancedBoxGroup("Window")]
        private TweenAnimationField _CloseAnimation;

        [ShowInInspector]
        [ReadOnly]
        [HideInEditorMode]
        [EnhancedBoxGroup("Window")]
        public WindowState State { get; private set; }

        public ScreenRoot Screen { get; private set; }
        public WindowDefinition Definition { get; private set; }
        public WindowHandle Handle { get; private set; }

        // Opened and focused at least once since this open
        public bool IsReady { get; private set; }

        private ViewStateRequests _requests;
        private Canvas _canvas;
        private CancellationTokenSource _openCancellation;

        internal void Setup(ScreenRoot screen, WindowDefinition definition)
        {
            Screen = screen;
            Definition = definition;
            _canvas = GetComponent<Canvas>();

            if (_requests != null)
                return;

            _requests = new ViewStateRequests(this);
            _requests.Set(LIFECYCLE_REQUEST, ViewState.Hidden);
            StateChanged += (_, _) => TryBecomeReady();
        }

        // Unity drops override sorting set while the canvas is inactive, so it is set with every order
        internal void SetSortingOrder(int sortingOrder)
        {
            SortingOrder = sortingOrder;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortingOrder;
        }

        internal void SetScreenCap(ViewState cap) => _requests.Set(SCREEN_REQUEST, cap);

        internal int SortingOrder { get; private set; }

        internal async UniTask OpenAsync(WindowHandle handle, object context, bool immediate, CancellationToken cancellationToken)
        {
            Handle = handle;
            IsReady = false;
            State = WindowState.Opening;
            _requests.Set(LIFECYCLE_REQUEST, ViewState.Visible);
            gameObject.SetActive(true);

            SetContext(context);
            OnWindowOpening();

            _openCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _openCancellation.Token;
            try
            {
                if (!immediate)
                {
                    // Closed look during the layout delay, so nothing flashes
                    _CloseAnimation?.SetToEnd();
                    for (var i = 0; i < _OpenFramesDelay; i++)
                        await UniTask.NextFrame(token);
                }

                await PlayAsync(_OpenAnimation, immediate, token);
            }
            catch (OperationCanceledException)
            {
                // Closed while opening, close takes over
                return;
            }
            finally
            {
                _openCancellation.Dispose();
                _openCancellation = null;
            }

            State = WindowState.Opened;
            _requests.Set(LIFECYCLE_REQUEST, ViewState.Focused);
            OnWindowOpened();
            handle.SetOpened();
            TryBecomeReady();
        }

        internal async UniTask CloseAsync(bool immediate, CancellationToken cancellationToken)
        {
            // Close plays from wherever the open animation stopped
            _openCancellation?.Cancel();

            State = WindowState.Closing;
            _requests.Set(LIFECYCLE_REQUEST, ViewState.Visible);
            OnWindowClosing();
            Handle.SetClosing();

            try
            {
                await PlayAsync(_CloseAnimation, immediate, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Screen destroyed, finish the state anyway
            }

            State = WindowState.Closed;
            _requests.Set(LIFECYCLE_REQUEST, ViewState.Hidden);
            OnWindowClosed();
            Handle = null;
        }

        private static async UniTask PlayAsync(TweenAnimationField animation, bool immediate, CancellationToken cancellationToken)
        {
            if (animation == null)
                return;

            var tween = animation.Play();
            if (immediate)
            {
                animation.Kill(true);
                return;
            }

            if (tween != null && tween.IsActive())
                await tween.ToUniTask(TweenCancelBehaviour.Kill, cancellationToken);
        }

        private void TryBecomeReady()
        {
            if (IsReady || State != WindowState.Opened || !FocusedInTree)
                return;

            IsReady = true;
            OnWindowReady();
        }

        // Closes with the given result, or default for typed windows
        public void Close(object result = null, bool immediate = false)
        {
            if (Handle != null)
                Screen.Close(Handle, result, immediate);
        }

        internal virtual void SetContext(object context) { }

        protected virtual void OnWindowOpening() { }
        protected virtual void OnWindowOpened() { }
        protected virtual void OnWindowReady() { }
        protected virtual void OnWindowClosing() { }
        protected virtual void OnWindowClosed() { }
    }

    public abstract class AWindow<TContext, TResult> : AWindow
    {
        public TContext Context { get; private set; }

        // Used when opened without a context
        protected virtual TContext DefaultContext => default;

        internal sealed override void SetContext(object context)
        {
            if (context != null && context is not TContext)
                Debug.LogError($"[Window] {name} expects {typeof(TContext).Name} context, got {context.GetType().Name}.", this);

            Context = context is TContext typed ? typed : DefaultContext;
            OnContextSet(Context);
        }

        // Called before OnWindowOpening
        protected virtual void OnContextSet(TContext context) { }

        public void Close(TResult result, bool immediate = false) => Close((object) result, immediate);
    }
}
