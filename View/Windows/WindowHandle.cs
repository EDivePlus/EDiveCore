// Author: František Holubec
// Created: 09.10.2026

using Cysharp.Threading.Tasks;

namespace EDIVE.View.Windows
{
    // One open request, tasks complete even when awaited late or when loading fails
    public class WindowHandle
    {
        private readonly UniTaskCompletionSource _opened = new();
        private readonly UniTaskCompletionSource _closing = new();
        private readonly UniTaskCompletionSource<object> _closed = new();

        public WindowDefinition Definition { get; }
        public ScreenRoot Screen { get; }

        // Null while loading or after a failed load
        public AWindow Window { get; internal set; }

        public bool IsClosing { get; private set; }
        public bool IsClosed { get; private set; }
        public object Result { get; internal set; }

        public UniTask Opened => _opened.Task;
        public UniTask Closing => _closing.Task;
        public UniTask<object> Closed => _closed.Task;

        internal bool IsCloseRequested { get; set; }

        internal WindowHandle(ScreenRoot screen, WindowDefinition definition)
        {
            Screen = screen;
            Definition = definition;
        }

        public void Close(object result = null, bool immediate = false) => Screen.Close(this, result, immediate);

        internal void SetOpened() => _opened.TrySetResult();

        internal void SetClosing()
        {
            IsClosing = true;
            _closing.TrySetResult();
        }

        internal void SetClosed()
        {
            IsClosing = true;
            IsClosed = true;
            _opened.TrySetResult();
            _closing.TrySetResult();
            _closed.TrySetResult(Result);
        }
    }

    public class WindowHandle<TResult> : WindowHandle
    {
        internal WindowHandle(ScreenRoot screen, WindowDefinition definition) : base(screen, definition) { }

        // Default when closed without a result or with a different type
        public async UniTask<TResult> GetResult()
        {
            var result = await Closed;
            return result is TResult typed ? typed : default;
        }

        public void Close(TResult result, bool immediate = false) => Screen.Close(this, result, immediate);

        public UniTask<TResult>.Awaiter GetAwaiter() => GetResult().GetAwaiter();
    }
}
