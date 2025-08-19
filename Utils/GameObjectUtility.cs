// Author: František Holubec
// Created: 22.07.2025

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.External.DomainReloadHelper;
using EDIVE.NativeUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EDIVE.Utils
{
    public static class GameObjectUtility
    {
        public static T InstantiateInitialize<T>(T prefab, Action<T> initializer, Transform parent = null) where T : Object
        {
            var instance = Object.Instantiate(prefab, InactiveParent);
            initializer?.Invoke(instance);
            
            // Reparent if the instance wasn't moved already
            if (instance.TryGetGameObject(out var instanceGameObject) && instanceGameObject.transform.parent == InactiveParent)
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
