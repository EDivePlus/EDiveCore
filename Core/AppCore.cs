// Author: František Holubec
// Created: 09.03.2025

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core.Services;
using EDIVE.External.DomainReloadHelper;
using EDIVE.External.Promises;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using IServiceProvider = EDIVE.Core.Services.IServiceProvider;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Core
{
    [DefaultExecutionOrder(1000)]
    public class AppCore : MonoBehaviour
    {
        [HideLabel]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [ShowInInspector]
        private ServiceProvider _services = new();
        
        [ClearOnReload]
        private static AppCore _instance;
        
        private static int _mainThreadId;

        // Create fake instance when needed
        public static IServiceProvider Services => HasInstance ? Instance._services : MockServiceProvider.INSTANCE;
        public static bool IsLoaded => HasInstance && Instance._isLoaded;
        public static bool HasInstance => Application.isPlaying && _instance != null;
        public static bool IsMainThread => Equals(_mainThreadId, Thread.CurrentThread.ManagedThreadId);

        public Scene RootScene { get; private set; }
        
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

        private bool _isLoaded;
        private Promise _loadedPromise;

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

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            _services = null;
        }

        public void SetRootScene(Scene scene)
        {
            RootScene = scene;
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
    }
}
