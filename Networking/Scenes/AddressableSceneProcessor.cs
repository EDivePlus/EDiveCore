// Author: František Holubec

using System.Collections;
using System.Collections.Generic;
using FishNet.Managing.Scened;
using UnityEngine;
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
        private readonly Dictionary<UnityScene, AsyncOperationHandle<SceneInstance>> _loadedScenes = new();
        private readonly List<AsyncOperationHandle> _activeUnloads = new();

        public override void LoadStart(LoadQueueData queueData)
        {
            base.LoadStart(queueData);
            _activeLoads.Clear();
        }

        public override void LoadEnd(LoadQueueData queueData)
        {
            base.LoadEnd(queueData);
            _activeLoads.Clear();
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
                var handle = Addressables.LoadSceneAsync(sceneName, parameters, activateOnLoad: false);
                _activeLoads.Add(handle);
            }
            else
            {
                base.BeginLoadAsync(sceneName, parameters);
            }
        }

        public override void BeginUnloadAsync(UnityScene scene)
        {
            if (_loadedScenes.Remove(scene, out var handle))
            {
                if (handle.IsValid())
                {
                    var unloadHandle = Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true);
                    _activeUnloads.Add(unloadHandle);
                }
            }
            else
            {
                base.BeginUnloadAsync(scene);
            }
        }

        public override float GetPercentComplete()
        {
            var max = base.GetPercentComplete();

            foreach (var h in _activeLoads)
            {
                if (h.IsValid())
                    max = Mathf.Max(max, h.PercentComplete);
            }

            foreach (var h in _activeUnloads)
            {
                if (h.IsValid())
                    max = Mathf.Max(max, h.PercentComplete);
            }

            return max;
        }

        public override bool IsPercentComplete() => GetPercentComplete() >= 1f;

        public override UnityScene GetLastLoadedScene()
        {
            for (var i = _activeLoads.Count - 1; i >= 0; i--)
            {
                var h = _activeLoads[i];
                if (h.IsValid() && h.IsDone && h.Status == AsyncOperationStatus.Succeeded)
                    return h.Result.Scene;
            }

            return base.GetLastLoadedScene();
        }

        public override void AddLoadedScene(UnityScene scene)
        {
            base.AddLoadedScene(scene);

            foreach (var h in _activeLoads)
            {
                if (!h.IsValid() || !h.IsDone || h.Status != AsyncOperationStatus.Succeeded || h.Result.Scene != scene) 
                    continue;
                _loadedScenes[scene] = h;
                break;
            }
        }

        public override void ActivateLoadedScenes()
        {
            base.ActivateLoadedScenes();

            foreach (var h in _activeLoads)
            {
                if (!h.IsValid() || !h.IsDone) continue;
                if (h.Status != AsyncOperationStatus.Succeeded) continue;
                if (!h.Result.Scene.IsValid()) continue;

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