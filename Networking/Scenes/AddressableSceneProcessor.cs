// Author: František Holubec

using System.Collections;
using System.Collections.Generic;
using FishNet.Managing.Scened;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace EDIVE.Networking.Scenes
{
    public class AddressableSceneProcessor : DefaultSceneProcessor
    {
        private readonly List<AsyncOperationHandle<SceneInstance>> _activeLoads = new();
        private readonly List<AsyncOperationHandle<SceneInstance>> _activeUnloads = new();
        private readonly Dictionary<UnityScene, AsyncOperationHandle<SceneInstance>> _loadedScenes = new();

        private AsyncOperationHandle<SceneInstance> _currentLoad;
        private bool _currentIsAddressable;

        public override void LoadStart(LoadQueueData queueData)
        {
            base.LoadStart(queueData);
            _activeLoads.Clear();
            _currentLoad = default;
            _currentIsAddressable = false;
        }

        public override void LoadEnd(LoadQueueData queueData)
        {
            base.LoadEnd(queueData);
            _activeLoads.Clear();
            _currentLoad = default;
            _currentIsAddressable = false;
        }

        public override void UnloadStart(UnloadQueueData queueData)
        {
            base.UnloadStart(queueData);
            _activeUnloads.Clear();
        }

        public override void BeginLoadAsync(string sceneName, LoadSceneParameters parameters)
        {
            if (TryLocateAddressableScene(sceneName))
            {
                _currentLoad = Addressables.LoadSceneAsync(sceneName, parameters, activateOnLoad: false);
                _currentIsAddressable = true;
                _activeLoads.Add(_currentLoad);
            }
            else
            {
                _currentLoad = default;
                _currentIsAddressable = false;
                base.BeginLoadAsync(sceneName, parameters);
            }
        }

        public override void BeginUnloadAsync(UnityScene scene)
        {
            if (_loadedScenes.Remove(scene, out var handle) && handle.IsValid())
            {
                var unload = Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true);
                _activeUnloads.Add(unload);
            }
            else
            {
                base.BeginUnloadAsync(scene);
            }
        }

        public override float GetPercentComplete()
        {
            if (_currentIsAddressable)
                return _currentLoad.IsValid() ? _currentLoad.PercentComplete : 1f;
            return base.GetPercentComplete();
        }

        public override bool IsPercentComplete() => GetPercentComplete() >= 0.9f;

        public override UnityScene GetLastLoadedScene()
        {
            if (_currentIsAddressable && _currentLoad.IsValid() && _currentLoad.Result.Scene.IsValid())
                return _currentLoad.Result.Scene;
            return base.GetLastLoadedScene();
        }

        public override void AddLoadedScene(UnityScene scene)
        {
            base.AddLoadedScene(scene);
            if (_currentIsAddressable && _currentLoad.IsValid() && _currentLoad.Result.Scene == scene)
                _loadedScenes[scene] = _currentLoad;
        }

        public override void ActivateLoadedScenes()
        {
            base.ActivateLoadedScenes();
            foreach (var h in _activeLoads)
            {
                if (h.IsValid() && h.Result.Scene.IsValid())
                    h.Result.ActivateAsync();
            }
        }

        public override IEnumerator AsyncsIsDone()
        {
            yield return base.AsyncsIsDone();

            bool pending;
            do
            {
                pending = false;

                foreach (var h in _activeLoads)
                {
                    if (h.IsValid() && !h.IsDone)
                    {
                        pending = true;
                        break;
                    }
                }

                if (!pending)
                {
                    foreach (var h in _activeUnloads)
                    {
                        if (h.IsValid() && !h.IsDone)
                        {
                            pending = true;
                            break;
                        }
                    }
                }

                if (pending)
                    yield return null;

            } while (pending);
        }

        private static bool TryLocateAddressableScene(string sceneName)
        {
            foreach (var locator in Addressables.ResourceLocators)
            {
                if (locator.Locate(sceneName, typeof(SceneInstance), out _))
                    return true;
            }
            return false;
        }
    }
}
