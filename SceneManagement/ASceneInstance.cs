// Author: František Holubec
// Created: 11.04.2025

using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.SceneManagement
{
    public abstract class ASceneInstance
    {
        [ShowInInspector]
        public abstract ASceneDefinition BaseDefinition { get; }

        [ShowInInspector]
        public SceneLoadState LoadState { get; private set; } = SceneLoadState.NotLoaded;
        public abstract Scene Scene { get; }

        [Button]
        public async UniTask Load(bool setActive = true)
        {
            if (!BaseDefinition.IsValid())
            {
                Debug.LogError($"Scene definition {BaseDefinition.UniqueID} is invalid");
                return;
            }
            
            if (LoadState != SceneLoadState.NotLoaded)
            {
                Debug.LogError($"Cannot load scene in state {LoadState}");
                return;
            }
          
            LoadState = SceneLoadState.Loading;
            try
            {
                await LoadOperation();
                Debug.Log($"Scene {BaseDefinition.UniqueID} loaded");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                LoadState = SceneLoadState.NotLoaded;
            }
            
            if (Scene.isLoaded) 
                LoadState = SceneLoadState.Loaded;

            if (setActive) SetActive();
        }

        [Button]
        public async UniTask Unload()
        {
            if (LoadState != SceneLoadState.Loaded)
            {
                Debug.LogError($"Cannot load scene in state {LoadState}");
                return;
            }

            if (!Scene.isLoaded)
            {
                Debug.LogError("The scene is not loaded in unity, something went wrong");
                return;
            }
            
            LoadState = SceneLoadState.Unloading;
            try
            {
                await UnloadOperation();
                Debug.Log($"Scene {BaseDefinition.UniqueID} unloaded");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
            LoadState = SceneLoadState.NotLoaded;
        }

        [Button]
        public void SetActive()
        {
            if (LoadState != SceneLoadState.Loaded)
            {
                Debug.LogError("Cannot set non-loaded scene active!");
                return;
            }
            SceneManager.SetActiveScene(Scene);
            Debug.Log($"Scene {BaseDefinition.UniqueID} set active");
        }
        
        protected abstract UniTask LoadOperation();
        protected abstract UniTask UnloadOperation();
    }
    
    public abstract class ASceneInstance<TDefinition> : ASceneInstance where TDefinition : ASceneDefinition
    {
        public TDefinition Definition { get; }
        public override ASceneDefinition BaseDefinition => Definition;

        protected ASceneInstance(TDefinition definition)
        {
            Definition = definition;
        }
    }
}
