// Author: František Holubec
// Created: 09.03.2025

using System;
using System.Threading;
using EDIVE.External.DomainReloadHelper;
using EDIVE.External.Promises;
using UnityEngine;

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

        [ClearOnReload]
        private static bool _applicationIsQuitting;
        
        public static ServiceProvider Services => Instance._services ??= new ServiceProvider();
        public static bool IsLoaded => HasInstance && Instance._isLoaded;
        public static bool HasInstance => Application.isPlaying && !_applicationIsQuitting && _instance != null;
        public static bool IsMainThread => Equals(_mainThreadId, Thread.CurrentThread.ManagedThreadId);
        
        public static AppCore Instance
        {
            get
            {
                if (_instance != null || !Application.isPlaying)
                    return _instance;

                if (_applicationIsQuitting)
                {
                    Debug.LogWarning("[AppCore] Application is quitting, instance will be destroyed.");
                    return null;
                }

                Debug.LogError("[AppCore] Core not initialized, make sure core is loaded first!");
                return null;
            }
        }

        private void Awake()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _applicationIsQuitting = false;
            
            if (_instance != null && !ReferenceEquals(this, _instance))
            {
                Debug.LogError("[AppCore] Instance was already created!", this);
                Destroy(gameObject);
            }

            DebugLite.Log("[AppCore] Initializing");
        }

        private void OnDestroy()
        {
            DebugLite.LogWarning("[AppCore] Application is quitting!");
            _instance = null;
        }

        public static void SetLoadCompleted()
        {
            if (!HasInstance)
                return;

            Instance._isLoaded = true;
            Instance._loadedPromise?.Dispatch();
        }
        
        public void WhenLoaded(Action action)
        {
            if (IsLoaded)
            {
                action?.Invoke();
                return;
            }

            _loadedPromise ??= new Promise();
            _loadedPromise.Then(action);
        }
    }
}
