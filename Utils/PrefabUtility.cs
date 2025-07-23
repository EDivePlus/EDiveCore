// Author: František Holubec
// Created: 22.07.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.External.DomainReloadHelper;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EDIVE.NativeUtils
{
    public static class PrefabUtility
    {
        public static T InstantiateInitialize<T>(T prefab, Action<T> initializer, Transform parent = null) where T : Object
        {
            var instance = Object.Instantiate(prefab, InactiveParent);
            initializer?.Invoke(instance);
            if (instance.TryGetGameObject(out var instanceGameObject))
            {
                instanceGameObject.transform.SetParent(parent);
            }
            return instance;
        }
        
        public static async UniTask<T> InstantiateInitializeAsync<T>(T prefab, Action<T> initializer, Transform parent = null) where T : Object
        {
            var instance = (await Object.InstantiateAsync(prefab, InactiveParent))[0];
            initializer?.Invoke(instance);
            if (instance.TryGetGameObject(out var instanceGameObject))
            {
                instanceGameObject.transform.SetParent(parent);
            }
            return instance;
        }
        
        [ClearOnReload]
        private static GameObject _inactiveParentObj;
        
        public static Transform InactiveParent
        {
            get
            {
                if (_inactiveParentObj != null && (Application.isPlaying || _inactiveParentObj.scene == SceneManager.GetActiveScene()) && _inactiveParentObj.scene.isLoaded) 
                    return _inactiveParentObj.transform;
            
                var go = new GameObject("AssetUtils_InactivePrefabParent");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.SetActive(false);

                if (Application.isPlaying) 
                    Object.DontDestroyOnLoad(go);

                _inactiveParentObj = go;
                return _inactiveParentObj.transform;
            }
        }
    }
}
