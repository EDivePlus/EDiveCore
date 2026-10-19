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
using EDIVE.View.Windows.Screens;

namespace EDIVE.View.Windows
{
    // Window root, opened into a ScreenRoot. Screen, lifecycle and minimize cap its view state.
    [RequireComponent(typeof(Canvas))]
    public abstract class AWindow : ViewGroup, IViewEventHandler<CloseWindowEvent>
    {
        private const string SCREEN_REQUEST = "Screen";
        private const string LIFECYCLE_REQUEST = "Lifecycle";
        private const string MINIMIZE_REQUEST = "Minimize";

        [Tooltip("Lets layout settle before the open animation")]
        [SerializeField]
        [EnhancedBoxGroup("Window")]
        private int _OpenFramesDelay = 1;

        [Tooltip("Also plays on restore")]
        [SerializeField]
        [EnhancedBoxGroup("Window")]
        private TweenAnimationField _OpenAnimation;

        [Tooltip("Also plays on minimize")]
        [SerializeField]
        [EnhancedBoxGroup("Window")]
        private TweenAnimationField _CloseAnimation;

        [ShowInInspector]
        [ReadOnly]
        [HideInEditorMode]
        [EnhancedBoxGroup("Window")]
        public WindowState State { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        [HideInEditorMode]
        [EnhancedBoxGroup("Window")]
        public bool IsMinimized { get; private set; }

        public ScreenRoot Screen { get; private set; }
        public WindowDefinition Definition { get; private set; }
        public WindowHandle Handle { get; private set; }

        // Opened and focused at least once since this open
        public bool IsReady { get; private set; }

        private ViewStateRequests _requests;
        private Canvas _canvas;
        private CancellationTokenSource _animation;

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
            OnSetup();
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
            IsMinimized = false;
            State = WindowState.Opening;
            _requests.Remove(MINIMIZE_REQUEST);
            _requests.Set(LIFECYCLE_REQUEST, ViewState.Visible);
            gameObject.SetActive(true);

            SetContext(context);
            OnWindowOpening();

            var animation = BeginAnimation(cancellationToken);
            try
            {
                if (!immediate)
                {
                    // Closed look during the layout delay, so nothing flashes
                    _CloseAnimation?.SetToEnd();
                    for (var i = 0; i < _OpenFramesDelay; i++)
                        await UniTask.NextFrame(animation.Token);
                }

                await PlayAsync(_OpenAnimation, immediate, animation.Token);
            }
            catch (OperationCanceledException)
            {
                // Closed or minimized while opening, they take over
                return;
            }
            finally
            {
                EndAnimation(animation);
            }

            CompleteOpen();
        }

        private void CompleteOpen()
        {
            State = WindowState.Opened;
            _requests.Set(LIFECYCLE_REQUEST, ViewState.Focused);
            OnWindowOpened();
            Handle.SetOpened();
            TryBecomeReady();
        }

        // Hidden but running, plays the close animation
        internal async UniTask MinimizeAsync(bool immediate, CancellationToken cancellationToken)
        {
            if (IsMinimized || State is WindowState.Closing or WindowState.Closed)
                return;

            IsMinimized = true;
            // Drawn but without input while it hides, set first so a pending open doesn't become ready
            _requests.Set(MINIMIZE_REQUEST, ViewState.Visible);
            if (State == WindowState.Opening)
                CompleteOpen();

            OnWindowMinimizing();

            var animation = BeginAnimation(cancellationToken);
            try
            {
                await PlayAsync(_CloseAnimation, immediate, animation.Token);
            }
            catch (OperationCanceledException)
            {
                // Restored or closed meanwhile
                return;
            }
            finally
            {
                EndAnimation(animation);
            }

            _requests.Set(MINIMIZE_REQUEST, ViewState.Hidden);
            OnWindowMinimized();
        }

        // Shown again, plays the open animation from wherever minimize stopped
        internal async UniTask RestoreAsync(bool immediate, CancellationToken cancellationToken)
        {
            if (!IsMinimized || State != WindowState.Opened)
                return;

            IsMinimized = false;
            _requests.Set(MINIMIZE_REQUEST, ViewState.Visible);
            OnWindowRestoring();

            var animation = BeginAnimation(cancellationToken);
            try
            {
                await PlayAsync(_OpenAnimation, immediate, animation.Token);
            }
            catch (OperationCanceledException)
            {
                // Minimized or closed meanwhile
                return;
            }
            finally
            {
                EndAnimation(animation);
            }

            _requests.Remove(MINIMIZE_REQUEST);
            OnWindowRestored();
        }

        internal async UniTask CloseAsync(bool immediate, CancellationToken cancellationToken)
        {
            // Close plays from wherever the open or minimize animation stopped, a hidden window just closes
            _animation?.Cancel();
            immediate |= IsMinimized;

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
            IsMinimized = false;
            _requests.Remove(MINIMIZE_REQUEST);
            _requests.Set(LIFECYCLE_REQUEST, ViewState.Hidden);
            OnWindowClosed();
            Handle = null;
        }

        // One animation at a time, a new one cancels the running one
        private CancellationTokenSource BeginAnimation(CancellationToken cancellationToken)
        {
            _animation?.Cancel();
            _animation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return _animation;
        }

        private void EndAnimation(CancellationTokenSource animation)
        {
            if (_animation == animation)
                _animation = null;
            animation.Dispose();
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

        // Asks CanCloseAsync first, for user closes like a close button
        public UniTask<bool> TryCloseAsync(object result = null, WindowCloseReason reason = WindowCloseReason.Code)
        {
            return Handle != null ? Screen.TryCloseAsync(Handle, result, reason) : UniTask.FromResult(false);
        }

        public void Minimize()
        {
            if (Handle != null)
                Screen.Minimize(Handle);
        }

        // Selects pages in the switches below, e.g. "audio/advanced"
        public bool Navigate(string path) => ViewSwitch.Navigate(this, path);

        // Not opened by a screen, the event goes further up
        bool IViewEventHandler<CloseWindowEvent>.HandleViewEvent(CloseWindowEvent viewEvent)
        {
            if (Handle == null)
                return false;

            Close(viewEvent.Result, viewEvent.Immediate);
            return true;
        }

        internal UniTask<bool> CheckCanCloseAsync(WindowCloseReason reason) => CanCloseAsync(reason);

        internal bool HandleBack() => OnBack();

        // Guard for back, backdrop and TryCloseAsync. Plain Close skips it.
        protected virtual UniTask<bool> CanCloseAsync(WindowCloseReason reason) => UniTask.FromResult(true);

        // True when handled, the back goes no further
        protected virtual bool OnBack()
        {
            switch (Definition.Back)
            {
                case WindowBackAction.Close:
                    TryCloseAsync(null, WindowCloseReason.Back).Forget();
                    return true;
                case WindowBackAction.Minimize:
                    Minimize();
                    return true;
                default:
                    return false;
            }
        }

        internal virtual void SetContext(object context) { }

        // Once per instance, the definition is set
        protected virtual void OnSetup() { }

        protected virtual void OnWindowOpening() { }
        protected virtual void OnWindowOpened() { }
        protected virtual void OnWindowReady() { }
        protected virtual void OnWindowMinimizing() { }
        protected virtual void OnWindowMinimized() { }
        protected virtual void OnWindowRestoring() { }
        protected virtual void OnWindowRestored() { }
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
