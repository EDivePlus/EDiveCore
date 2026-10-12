// Author: František Holubec
// Created: 09.10.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.Activations;
using EDIVE.View.ViewTree;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.View.Windows
{
    // Root of one screen, hosts its windows. Orders them by layer and open order, caps their view state:
    // top is Focused, below a modal Visible, below an opened opaque window Hidden.
    public class ScreenRoot : ViewGroup
    {
        [Serializable]
        public class LayerContainer
        {
            [Required]
            public WindowLayer Layer;

            [Required]
            public Transform Container;
        }

        [SerializeField]
        [EnhancedBoxGroup("Windows")]
        private List<LayerContainer> _Layers = new();

        // Sorted just below the top modal, needs a Canvas
        [SerializeField]
        [EnhancedBoxGroup("Windows")]
        private ViewGroup _Backdrop;

        [SerializeField]
        [EnhancedBoxGroup("Windows")]
        private Button _BackdropButton;

        // Instantiated into the pool on start, only for Keep After Close definitions
        [SerializeField]
        [EnhancedBoxGroup("Windows")]
        private List<WindowDefinition> _Preload = new();

        // Definitions Navigate can open by their route ID
        [SerializeField]
        [EnhancedBoxGroup("Windows")]
        private List<WindowDefinition> _Routes = new();

        [SerializeReference]
        [EnhancedBoxGroup("Windows")]
        private IActivation _BackActivation;

        private readonly List<WindowHandle> _handles = new();
        private readonly Dictionary<WindowDefinition, Stack<AWindow>> _pool = new();
        private readonly List<WindowHandle> _drawOrder = new();
        private Transform _poolRoot;
        private Canvas _backdropCanvas;
        private WindowHandle _topModal;
        private IDisposable _backListener;

        // Open order, includes windows still loading or closing
        public IReadOnlyList<WindowHandle> Handles => _handles;

        protected override void Awake()
        {
            // Inactive root, windows get set up before they ever enable
            var poolObject = new GameObject("Pool");
            poolObject.SetActive(false);
            _poolRoot = poolObject.transform;
            _poolRoot.SetParent(transform, false);

            if (_Backdrop != null)
            {
                _backdropCanvas = _Backdrop.GetComponent<Canvas>();
                _Backdrop.SetState(ViewState.Hidden);
            }

            if (_BackdropButton != null)
                _BackdropButton.onClick.AddListener(OnBackdropClicked);

            base.Awake();
        }

        private void Start()
        {
            foreach (var definition in _Preload)
            {
                if (definition != null && definition.KeepAfterClose)
                    PreloadAsync(definition, destroyCancellationToken).Forget();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _backListener = _BackActivation.RegisterDisposable(OnBackActivated);
        }

        protected override void OnDisable()
        {
            _backListener?.Dispose();
            _backListener = null;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (_BackdropButton != null)
                _BackdropButton.onClick.RemoveListener(OnBackdropClicked);

            // Each instance holds its definition prefab
            foreach (var handle in _handles)
            {
                if (handle.Window != null)
                    handle.Definition.ReleasePrefab();
                if (!handle.IsClosing && !handle.IsCloseRequested)
                    handle.CloseReason = WindowCloseReason.ScreenDestroyed;
                handle.SetClosed();
            }
            _handles.Clear();

            foreach (var (definition, windows) in _pool)
            {
                for (var i = 0; i < windows.Count; i++)
                    definition.ReleasePrefab();
            }
            _pool.Clear();

            base.OnDestroy();
        }

        public WindowHandle Open(WindowDefinition definition, object context = null, bool immediate = false)
        {
            return Open(new WindowHandle(this, definition), context, immediate);
        }

        public WindowHandle<TResult> Open<TResult>(WindowDefinition definition, object context = null, bool immediate = false)
        {
            return (WindowHandle<TResult>) Open(new WindowHandle<TResult>(this, definition), context, immediate);
        }

        // First segment is a route ID from Routes, the rest selects pages in the window, e.g. "settings/audio"
        public WindowHandle Navigate(string path, object context = null, bool immediate = false)
        {
            var separator = path?.IndexOf('/') ?? -1;
            var routeID = separator >= 0 ? path.Substring(0, separator) : path;
            var pagePath = separator >= 0 ? path.Substring(separator + 1) : null;

            var definition = FindRoute(routeID);
            if (definition == null)
            {
                Debug.LogError($"[ScreenRoot] {name} has no route '{routeID}'.", this);
                return Fail(new WindowHandle(this, null));
            }

            var handle = Open(definition, context, immediate);
            if (!string.IsNullOrEmpty(pagePath))
                handle.Navigate(pagePath);
            return handle;
        }

        // Closes the handle, the definition opens right above it
        public WindowHandle Replace(WindowHandle handle, WindowDefinition definition, object context = null, bool immediate = false)
        {
            return Replace(handle, new WindowHandle(this, definition), context, immediate);
        }

        public WindowHandle<TResult> Replace<TResult>(WindowHandle handle, WindowDefinition definition, object context = null, bool immediate = false)
        {
            return (WindowHandle<TResult>) Replace(handle, new WindowHandle<TResult>(this, definition), context, immediate);
        }

        // Top of its layer
        public void BringToFront(WindowHandle handle)
        {
            if (handle == null || handle.Screen != this || handle.IsClosing || !_handles.Remove(handle))
                return;

            _handles.Add(handle);
            Refresh();
        }

        // No guard, see TryCloseAsync
        public void Close(WindowHandle handle, object result = null, bool immediate = false)
        {
            Close(handle, result, WindowCloseReason.Code, immediate);
        }

        // Asks the window first, false when it refused or is already closing
        public async UniTask<bool> TryCloseAsync(WindowHandle handle, object result = null, WindowCloseReason reason = WindowCloseReason.Code)
        {
            if (handle == null || handle.Screen != this || handle.IsClosing || handle.IsGuarding)
                return false;

            var window = handle.Window;
            if (window != null)
            {
                bool canClose;
                handle.IsGuarding = true;
                try
                {
                    canClose = await window.CheckCanCloseAsync(reason);
                }
                catch (Exception e)
                {
                    if (e is not OperationCanceledException)
                        Debug.LogException(e, window);
                    canClose = false;
                }
                finally
                {
                    handle.IsGuarding = false;
                }

                if (!canClose || this == null || handle.IsClosing)
                    return false;
            }

            Close(handle, result, reason, false);
            return true;
        }

        public void CloseAll(bool immediate = false)
        {
            for (var i = _handles.Count - 1; i >= 0; i--)
                Close(_handles[i], null, immediate);
        }

        // Windows drawn above the handle, any layer
        public void CloseAbove(WindowHandle handle, bool immediate = false)
        {
            var index = _handles.IndexOf(handle);
            if (index < 0)
                return;

            var sortingOrder = handle.Definition.Layer.SortingOrder;
            for (var i = _handles.Count - 1; i > index; i--)
            {
                if (_handles[i].Definition.Layer.SortingOrder >= sortingOrder)
                    Close(_handles[i], null, immediate);
            }

            for (var i = index - 1; i >= 0; i--)
            {
                if (_handles[i].Definition.Layer.SortingOrder > sortingOrder)
                    Close(_handles[i], null, immediate);
            }
        }

        public void CloseLayer(WindowLayer layer, bool immediate = false)
        {
            for (var i = _handles.Count - 1; i >= 0; i--)
            {
                if (_handles[i].Definition.Layer == layer)
                    Close(_handles[i], null, immediate);
            }
        }

        // Top focused window first, the first one that handles it wins. A modal stops it, windows below are not focused.
        public bool Back()
        {
            if (!FocusedInTree)
                return false;

            for (var i = _drawOrder.Count - 1; i >= 0; i--)
            {
                var window = _drawOrder[i].Window;
                if (window != null && window.FocusedInTree && window.HandleBack())
                    return true;
            }
            return false;
        }

        private WindowHandle Open(WindowHandle handle, object context, bool immediate)
        {
            var definition = handle.Definition;
            if (definition == null || definition.Layer == null || GetContainer(definition.Layer) == null)
            {
                Debug.LogError($"[ScreenRoot] {name} can't open {(definition != null ? definition.name : "null")}, missing definition or its layer.", this);
                return Fail(handle);
            }

            switch (definition.InstancePolicy)
            {
                case WindowInstancePolicy.Single:
                    var open = FindOpen(definition);
                    if (open != null)
                        return Reopen(open, handle, context);
                    break;

                case WindowInstancePolicy.Replace:
                    for (var i = _handles.Count - 1; i >= 0; i--)
                    {
                        if (_handles[i].Definition == definition)
                            Close(_handles[i], null, WindowCloseReason.Replaced, immediate);
                    }
                    break;
            }

            handle.Context = context;
            _handles.Add(handle);
            OpenAsync(handle, immediate, destroyCancellationToken).Forget();
            return handle;
        }

        private WindowHandle Reopen(WindowHandle open, WindowHandle requested, object context)
        {
            if (!requested.GetType().IsInstanceOfType(open))
            {
                Debug.LogError($"[ScreenRoot] {name} has {open.Definition.name} open with another result type.", this);
                return Fail(requested);
            }

            if (context != null)
            {
                open.Context = context;
                if (open.Window != null)
                    open.Window.SetContext(context);
            }

            BringToFront(open);
            return open;
        }

        private WindowHandle Replace(WindowHandle handle, WindowHandle replacement, object context, bool immediate)
        {
            var index = handle != null && handle.Screen == this ? _handles.IndexOf(handle) : -1;
            if (index >= 0)
                Close(handle, null, WindowCloseReason.Replaced, immediate);

            var opened = Open(replacement, context, immediate);
            if (index < 0 || opened != replacement || opened.IsClosed || !_handles.Remove(opened))
                return opened;

            // Right above the old one, an immediate close already removed it
            var oldIndex = _handles.IndexOf(handle);
            _handles.Insert(oldIndex >= 0 ? oldIndex + 1 : Mathf.Min(index, _handles.Count), opened);
            Refresh();
            return opened;
        }

        private async UniTaskVoid OpenAsync(WindowHandle handle, bool immediate, CancellationToken cancellationToken)
        {
            AWindow window;
            try
            {
                window = await TakeWindowAsync(handle.Definition, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (window == null)
            {
                Debug.LogError($"[ScreenRoot] {name} failed to load {handle.Definition.name}.", this);
                _handles.Remove(handle);
                Fail(handle);
                return;
            }

            // Closed while loading
            if (handle.IsCloseRequested)
            {
                ReturnWindow(handle.Definition, window);
                _handles.Remove(handle);
                handle.SetClosed();
                return;
            }

            handle.Window = window;
            window.transform.SetParent(GetContainer(handle.Definition.Layer), false);
            var opening = window.OpenAsync(handle, handle.Context, immediate, cancellationToken);

            // Window is active now, its switches are in the tree
            if (handle.PendingPath != null)
            {
                var path = handle.PendingPath;
                handle.PendingPath = null;
                window.Navigate(path);
            }

            Refresh();
            await opening;
            if (this != null)
                Refresh();
        }

        private void Close(WindowHandle handle, object result, WindowCloseReason reason, bool immediate)
        {
            if (handle == null || handle.Screen != this || handle.IsClosing || handle.IsCloseRequested)
                return;

            handle.Result = result;
            handle.CloseReason = reason;
            if (handle.Window == null)
            {
                handle.IsCloseRequested = true;
                return;
            }

            CloseAsync(handle, immediate, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid CloseAsync(WindowHandle handle, bool immediate, CancellationToken cancellationToken)
        {
            var window = handle.Window;
            var closing = window.CloseAsync(immediate, cancellationToken);
            Refresh();
            await closing;

            if (this == null)
                return;

            _handles.Remove(handle);
            ReturnWindow(handle.Definition, window);
            handle.Window = null;
            handle.SetClosed();
            Refresh();
        }

        private static WindowHandle Fail(WindowHandle handle)
        {
            handle.CloseReason = WindowCloseReason.Failed;
            handle.SetClosed();
            return handle;
        }

        private WindowHandle FindOpen(WindowDefinition definition)
        {
            for (var i = _handles.Count - 1; i >= 0; i--)
            {
                var handle = _handles[i];
                if (handle.Definition == definition && !handle.IsClosing && !handle.IsCloseRequested)
                    return handle;
            }
            return null;
        }

        private WindowDefinition FindRoute(string routeID)
        {
            if (string.IsNullOrEmpty(routeID))
                return null;

            foreach (var definition in _Routes)
            {
                if (definition != null && string.Equals(definition.UniqueID, routeID, StringComparison.Ordinal))
                    return definition;
            }
            return null;
        }

        private async UniTask<AWindow> TakeWindowAsync(WindowDefinition definition, CancellationToken cancellationToken)
        {
            if (_pool.TryGetValue(definition, out var pooled) && pooled.Count > 0)
                return pooled.Pop();

            var prefab = await definition.LoadPrefabAsync(cancellationToken);
            if (prefab == null)
                return null;

            if (this == null)
            {
                definition.ReleasePrefab();
                return null;
            }

            var window = Instantiate(prefab, _poolRoot, false);
            window.name = prefab.name;
            window.Setup(this, definition);
            return window;
        }

        private void ReturnWindow(WindowDefinition definition, AWindow window)
        {
            if (!definition.KeepAfterClose)
            {
                Destroy(window.gameObject);
                definition.ReleasePrefab();
                return;
            }

            window.transform.SetParent(_poolRoot, false);
            if (!_pool.TryGetValue(definition, out var pooled))
                _pool[definition] = pooled = new Stack<AWindow>();
            pooled.Push(window);
        }

        private async UniTaskVoid PreloadAsync(WindowDefinition definition, CancellationToken cancellationToken)
        {
            try
            {
                var window = await TakeWindowAsync(definition, cancellationToken);
                if (window != null)
                    ReturnWindow(definition, window);
            }
            catch (OperationCanceledException) { }
        }

        private Transform GetContainer(WindowLayer layer)
        {
            foreach (var entry in _Layers)
            {
                if (entry.Layer == layer)
                    return entry.Container;
            }
            return null;
        }

        // Sorting orders and view state caps from the top down
        private void Refresh()
        {
            _drawOrder.Clear();
            foreach (var handle in _handles)
            {
                if (handle.Window != null)
                    _drawOrder.Add(handle);
            }

            // Stable sort keeps open order inside a layer
            for (var i = 1; i < _drawOrder.Count; i++)
            {
                var current = _drawOrder[i];
                var j = i - 1;
                while (j >= 0 && _drawOrder[j].Definition.Layer.SortingOrder > current.Definition.Layer.SortingOrder)
                {
                    _drawOrder[j + 1] = _drawOrder[j];
                    j--;
                }
                _drawOrder[j + 1] = current;
            }

            // Two steps per window, the backdrop goes in between
            WindowLayer currentLayer = null;
            var sortingOrder = 0;
            foreach (var handle in _drawOrder)
            {
                if (handle.Definition.Layer != currentLayer)
                {
                    currentLayer = handle.Definition.Layer;
                    sortingOrder = currentLayer.SortingOrder;
                }
                sortingOrder += 2;
                handle.Window.SetSortingOrder(sortingOrder);
            }

            var cap = ViewState.Focused;
            _topModal = null;
            for (var i = _drawOrder.Count - 1; i >= 0; i--)
            {
                var handle = _drawOrder[i];
                var window = handle.Window;
                window.SetScreenCap(cap);

                // Closing windows already hand focus back
                if (window.State == WindowState.Closing)
                    continue;

                if (window.Definition.Modal)
                {
                    _topModal ??= handle;
                    if (cap > ViewState.Visible)
                        cap = ViewState.Visible;
                }

                if (window.Definition.Opaque && window.State == WindowState.Opened)
                    cap = ViewState.Hidden;
            }

            if (_Backdrop == null)
                return;

            if (_topModal != null && _backdropCanvas != null)
            {
                _backdropCanvas.overrideSorting = true;
                _backdropCanvas.sortingOrder = _topModal.Window.SortingOrder - 1;
            }
            _Backdrop.SetState(_topModal != null ? ViewState.Focused : ViewState.Hidden);
        }

        private void OnBackActivated() => Back();

        private void OnBackdropClicked()
        {
            if (_topModal != null && _topModal.Definition.CloseOnBackdrop)
                TryCloseAsync(_topModal, null, WindowCloseReason.Backdrop).Forget();
        }
    }
}
