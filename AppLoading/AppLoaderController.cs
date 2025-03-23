using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Finalizers;
using EDIVE.AppLoading.LoadItems;
using EDIVE.Core;
using EDIVE.External.Signals;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.AppLoading
{
    public class AppLoaderController : MonoBehaviour, IService
    {
        public const string PERSISTENT_SCENE_NAME = "PersistentStage";

        [SerializeField]
        [ShowCreateNew]
        private LoadSetupDefinition _Setup;

        private float _totalLoadWeight;

        private float _currentLoadCompletedWeight;
        private readonly List<ALoadItemDefinition> _currentLoadingItems = new();

        protected float? LoadTime { get; private set; }
        public LoadSetupDefinition Setup => _Setup;

        public Signal LoadCompletedSignal { get; } = new Signal();
        public Signal LoadFinalizedSignal { get; } = new Signal();

        // ReSharper disable once Unity.IncorrectMethodSignature
        [UsedImplicitly]
        private async UniTaskVoid Start()
        {
            AppCore.Services.Register(this);
            DebugLite.Log("[AppLoader] Initializing");
            LoadTime = null;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var prevRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var validLoadItems = Setup.GetValidLoadItems().ToList();
            _totalLoadWeight = CalculateTotalLoadWeight();

            var persistentScene = SceneManager.CreateScene(PERSISTENT_SCENE_NAME);
            SceneManager.SetActiveScene(persistentScene);

            var gameInstance = AppCore.Instance.gameObject;
            SceneManager.MoveGameObjectToScene(gameInstance, persistentScene);
            gameInstance.transform.SetAsFirstSibling();

            await OnLoadStarting();

            Setup.Initialize();

            _currentLoadCompletedWeight = 0f;
            _currentLoadingItems.Clear();

            foreach (var loadItem in validLoadItems)
                loadItem.LoadStartedSignal.AddListener(OnLoadStarted);

            await Setup.Load();

            Setup.Terminate();

            await OnLoadEnding();

            LoadTime = stopwatch.ElapsedMilliseconds;
            DebugLite.Log($"[AppLoader] Total load time: {LoadTime}ms");
            stopwatch.Stop();

            DebugLite.Log($"[AppLoader] Load Times:\n{string.Join("\n", validLoadItems.Select(l => $"{l.UniqueID}: {l.LoadTime}ms"))}");

            OnLoadCompleted();
            LoadCompletedSignal.Dispatch();

            var finalizer = ResolveLoadFinalizer();
            if (finalizer != null)
                await finalizer.TryFinalizeLoad();

            LoadFinalizedSignal.Dispatch();
            Application.runInBackground = prevRunInBackground;
            AppCore.SetLoadCompleted();

            DebugLite.Log("[AppLoader] Unloading scene");
            await SceneManager.UnloadSceneAsync(gameObject.scene);

            AppCore.Services.Unregister<AppLoaderController>();
            GC.Collect();
            DebugLite.Log("[AppLoader] Load completed");
        }

        protected virtual UniTask OnLoadStarting()
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnLoadEnding()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnLoadCompleted()
        {
            
        }

        protected virtual ILoadFinalizer ResolveLoadFinalizer()
        {
            return _Setup.Finalizer;
        }

        public float GetLoadingProgress()
        {
            if (_totalLoadWeight <= 0)
                return 0;

            var currentLoadWeight = _currentLoadCompletedWeight + _currentLoadingItems.Sum(i => i.GetCurrentLoadingWeight());
            if (AppCore.IsLoaded)
                return 1;

            return Mathf.Clamp(currentLoadWeight / _totalLoadWeight, 0, 0.99f);
        }

        private float CalculateTotalLoadWeight()
        {
            return _Setup.GetValidLoadItems().Sum(loadItem => loadItem.LoadWeight);
        }

        private void OnLoadStarted(ALoadItemDefinition loadItem)
        {
            _currentLoadingItems.Add(loadItem);
            loadItem.LoadStartedSignal.RemoveListener(OnLoadStarted);
            loadItem.LoadCompletedSignal.AddListener(OnLoadCompleted);
        }

        private void OnLoadCompleted(ALoadItemDefinition loadItem)
        {
            _currentLoadingItems.Remove(loadItem);
            _currentLoadCompletedWeight += loadItem.LoadWeight;
            loadItem.LoadCompletedSignal.RemoveListener(OnLoadCompleted);
        }
    }
}
