// Author: František Holubec
// Created: 09.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.VisualPresets.Presets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.Toasts
{
    // Timed toasts stacked in a container, rest waits in a queue. Find it with FindInViewTree.
    public class ToastManager : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private ToastDisplay _DisplayPrefab;

        [Tooltip("Layout group, newest is last")]
        [SerializeField]
        [Required]
        private RectTransform _Container;

        [SerializeField]
        [Min(1)]
        private int _MaxVisible = 3;

        [SerializeField]
        private float _DefaultDuration = 3f;

        private readonly List<ToastHandle> _visible = new();
        private readonly List<ToastHandle> _queue = new();
        private readonly Dictionary<ToastDisplay, Stack<ToastDisplay>> _pools = new();

        // Same preset already shown restarts its timer, already queued returns the queued one. Prefab overrides the default display
        public ToastHandle Show(VisualPreset preset, float? duration = null, ToastDisplay prefab = null)
        {
            var existing = FindSame(_visible, preset);
            if (existing != null)
            {
                if (duration.HasValue)
                    existing.Duration = duration.Value;
                existing.Remaining = existing.Duration;
                return existing;
            }

            existing = FindSame(_queue, preset);
            if (existing != null)
            {
                if (duration.HasValue)
                    existing.Duration = existing.Remaining = duration.Value;
                return existing;
            }

            var handle = new ToastHandle(this, preset, duration ?? _DefaultDuration, prefab ? prefab : _DisplayPrefab);
            _queue.Add(handle);
            ShowQueued();
            return handle;
        }

        public void DismissAll()
        {
            foreach (var handle in _queue)
                handle.IsDismissed = true;
            _queue.Clear();

            foreach (var handle in _visible.ToArray())
                Dismiss(handle);
        }

        internal void Dismiss(ToastHandle handle)
        {
            if (handle.IsDismissed)
                return;

            handle.IsDismissed = true;
            if (_queue.Remove(handle))
                return;

            if (_visible.Contains(handle))
                HideAsync(handle).Forget();
        }

        private void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            for (var i = _visible.Count - 1; i >= 0; i--)
            {
                var handle = _visible[i];
                if (handle.IsHiding || handle.Duration <= 0f)
                    continue;

                handle.Remaining -= deltaTime;
                if (handle.Remaining <= 0f)
                    Dismiss(handle);
            }
        }

        private void ShowQueued()
        {
            while (_queue.Count > 0 && _visible.Count < _MaxVisible)
            {
                var handle = _queue[0];
                _queue.RemoveAt(0);

                var display = GetPool(handle.Prefab).TryPop(out var pooled) ? pooled : Instantiate(handle.Prefab, _Container, false);
                display.transform.SetAsLastSibling();
                display.Show(handle.Preset, handle.Dismiss);

                handle.Display = display;
                _visible.Add(handle);
            }
        }

        private async UniTaskVoid HideAsync(ToastHandle handle)
        {
            handle.IsHiding = true;
            try
            {
                await handle.Display.HideAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _visible.Remove(handle);
            GetPool(handle.Prefab).Push(handle.Display);
            handle.Display = null;
            ShowQueued();
        }

        private Stack<ToastDisplay> GetPool(ToastDisplay prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
                _pools[prefab] = pool = new Stack<ToastDisplay>();
            return pool;
        }

        private static ToastHandle FindSame(List<ToastHandle> handles, VisualPreset preset)
        {
            foreach (var handle in handles)
            {
                if (!handle.IsDismissed && IsSame(handle.Preset, preset))
                    return handle;
            }
            return null;
        }

        private static bool IsSame(VisualPreset a, VisualPreset b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null)
                return false;
            return a.EnumerateValidRecords().SequenceEqual(b.EnumerateValidRecords());
        }
    }
}
