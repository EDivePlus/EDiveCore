// Author: František Holubec
// Created: 09.10.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.OdinExtensions.Attributes;
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

        private readonly List<WindowHandle> _handles = new();
        private readonly Dictionary<WindowDefinition, Stack<AWindow>> _pool = new();
        private readonly List<WindowHandle> _orderBuffer = new();
        private Transform _poolRoot;
        private Canvas _backdropCanvas;
        private WindowHandle _topModal;

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

        protected override void OnDestroy()
        {
            if (_BackdropButton != null)
                _BackdropButton.onClick.RemoveListener(OnBackdropClicked);

            // Each instance holds its definition prefab
            foreach (var handle in _handles)
            {
                if (handle.Window != null)
                    handle.Definition.ReleasePrefab();
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

        private WindowHandle Open(WindowHandle handle, object context, bool immediate)
        {
            if (handle.Definition == null || handle.Definition.Layer == null || GetContainer(handle.Definition.Layer) == null)
            {
                Debug.LogError($"[ScreenRoot] {name} can't open {(handle.Definition != null ? handle.Definition.name : "null")}, missing definition or its layer.", this);
                handle.SetClosed();
                return handle;
            }

            _handles.Add(handle);
            OpenAsync(handle, context, immediate, destroyCancellationToken).Forget();
            return handle;
        }

        private async UniTaskVoid OpenAsync(WindowHandle handle, object context, bool immediate, CancellationToken cancellationToken)
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
                handle.SetClosed();
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
            var opening = window.OpenAsync(handle, context, immediate, cancellationToken);
            Refresh();
            await opening;
            if (this != null)
                Refresh();
        }

        public void Close(WindowHandle handle, object result = null, bool immediate = false)
        {
            if (handle == null || handle.Screen != this || handle.IsClosing)
                return;

            handle.Result = result;
            if (handle.Window == null)
            {
                handle.IsCloseRequested = true;
                return;
            }

            CloseAsync(handle, immediate, destroyCancellationToken).Forget();
        }

        public void CloseAll(bool immediate = false)
        {
            for (var i = _handles.Count - 1; i >= 0; i--)
                Close(_handles[i], null, immediate);
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
            _orderBuffer.Clear();
            foreach (var handle in _handles)
            {
                if (handle.Window != null)
                    _orderBuffer.Add(handle);
            }

            // Stable sort keeps open order inside a layer
            for (var i = 1; i < _orderBuffer.Count; i++)
            {
                var current = _orderBuffer[i];
                var j = i - 1;
                while (j >= 0 && _orderBuffer[j].Definition.Layer.SortingOrder > current.Definition.Layer.SortingOrder)
                {
                    _orderBuffer[j + 1] = _orderBuffer[j];
                    j--;
                }
                _orderBuffer[j + 1] = current;
            }

            // Two steps per window, the backdrop goes in between
            WindowLayer currentLayer = null;
            var sortingOrder = 0;
            foreach (var handle in _orderBuffer)
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
            for (var i = _orderBuffer.Count - 1; i >= 0; i--)
            {
                var handle = _orderBuffer[i];
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

        private void OnBackdropClicked()
        {
            if (_topModal != null && _topModal.Definition.CloseOnBackdrop)
                Close(_topModal);
        }
    }
}
