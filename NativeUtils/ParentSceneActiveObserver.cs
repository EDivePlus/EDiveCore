// Author: František Holubec
// Created: 21.08.2025

using System;
using EDIVE.NativeUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.Utils
{
    public abstract class ParentSceneActiveObserver : MonoBehaviour
    {
        public bool IsParentSceneActive { get; private set; }
        public event Action<bool> ParentSceneActiveChanged;

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            UpdateActiveSceneState();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            IsParentSceneActive = false;
        }

        private void HandleActiveSceneChanged(Scene oldScene, Scene newScene) { UpdateActiveSceneState(); }

        private void UpdateActiveSceneState()
        {
            var currentlyActive = gameObject.scene == SceneManager.GetActiveScene();
            if (IsParentSceneActive == currentlyActive)
                return;

            IsParentSceneActive = currentlyActive;
            ParentSceneActiveChanged?.Invoke(IsParentSceneActive);
        }
    }

    public static class ParentSceneActiveObserverExtensions
    {
        public static void AddParentSceneActiveChangeListener(this GameObject go, Action<bool> listener, bool checkImmediately = false)
        {
            if (go == null || listener == null)
                return;

            var monitor = go.GetOrAddComponent<ParentSceneActiveObserver>();
            monitor.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
            monitor.ParentSceneActiveChanged -= listener;
            monitor.ParentSceneActiveChanged += listener;

            if (checkImmediately)
            {
                listener.Invoke(monitor.IsParentSceneActive);
            }
        }

        public static void RemoveParentSceneActiveChangeListener(this GameObject go, Action<bool> listener)
        {
            if (go == null || listener == null || !go.TryGetComponent<ParentSceneActiveObserver>(out var monitor))
                return;

            monitor.ParentSceneActiveChanged -= listener;
        }
    }
}
