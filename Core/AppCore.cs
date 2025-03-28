// Author: František Holubec
// Created: 09.03.2025

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.External.DomainReloadHelper;
using EDIVE.External.Promises;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Core
{
    public class AppCore : MonoBehaviour
    { 
        private bool _isLoaded;
        private Promise _loadedPromise;
        private ServiceProvider _services;
        
        [ClearOnReload]
        private static AppCore _instance;
        
        private static int _mainThreadId;
        
        public static ServiceProvider Services => Instance._services ??= new ServiceProvider();
        public static bool IsLoaded => HasInstance && Instance._isLoaded;
        public static bool HasInstance => Application.isPlaying && _instance != null;
        public static bool IsMainThread => Equals(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
        
        public static AppCore Instance
        {
            get
            {
                if (_instance != null || !Application.isPlaying)
                    return _instance;

                Debug.LogError("[AppCore] Core not initialized, make sure core is loaded first!");
                return null;
            }
        }

        private void Awake()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            
            if (_instance != null && !ReferenceEquals(this, _instance))
            {
                Debug.LogError("[AppCore] Instance was already created!", this);
                Destroy(gameObject);
            }
            _instance = this;
            DebugLite.Log("[AppCore] Initializing");
        }

        public static void SetLoadCompleted()
        {
            if (!HasInstance)
                return;

            Instance._isLoaded = true;
            Instance._loadedPromise?.Dispatch();
        }
        
        public static void WhenLoaded(Action action)
        {
            if (!HasInstance)
                return;
            
            if (IsLoaded)
            {
                action?.Invoke();
                return;
            }

            Instance._loadedPromise ??= new Promise();
            Instance._loadedPromise.Then(action);
        }

        public static UniTask AwaitLoaded()
        {
            var source = new UniTaskCompletionSource();
            WhenLoaded(() => source.TrySetResult());
            return source.Task;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void InitializeEditor()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                _instance = null;
        }
#endif
    }
}
