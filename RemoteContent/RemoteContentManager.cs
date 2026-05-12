// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core;
using EDIVE.ServiceHub.RemoteContent.Handlers;
using FishNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.ServiceHub.RemoteContent
{
    public class RemoteContentManager : ALoadableServiceBehaviour<RemoteContentManager>
    {
        [SerializeField]
        private List<ARemoteContentHandler> _HandlerPrefabs = new();

        [SerializeField]
        private float _SpawnDistance = 1.5f;

        [SerializeField]
        private float _SpawnHeight;
        
        private readonly Dictionary<string, string> _shareTokenCache = new();

        public ARemoteContentHandler FocusedHandler { get; private set; }
        public event Action<ARemoteContentHandler> FocusedHandlerChanged;

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            return UniTask.CompletedTask;
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(ServiceHubManager));
        }
        
        public async UniTask SpawnHandlerAsync(ContentItemInfo content)
        {
            if (content == null || string.IsNullOrEmpty(content.Id))
            {
                Debug.LogError("[RemoteContentManager] SpawnHandler called with null or invalid content");
                return;
            }

            var handlerPrefab = _HandlerPrefabs.Find(r => r != null && r.IsValidFor(content));
            if (handlerPrefab == null)
            {
                Debug.LogError($"[RemoteContentManager] No handler found for media type '{content.MediaTypeKey}'");
                return;
            }

            if (!_shareTokenCache.TryGetValue(content.Id, out var shareToken))
            {
                var contentApi = AppCore.Services.Get<ServiceHubManager>().RemoteContent;
                var shareResponse = await contentApi.CreateContentShareAsync(content.Id);
                if (!shareResponse.IsSuccess || shareResponse.Result == null || string.IsNullOrEmpty(shareResponse.Result.Token))
                {
                    Debug.LogError($"[RemoteContentManager] Failed to create share for '{content.Id}': {shareResponse.ErrorMessage}");
                    return;
                }
                shareToken = shareResponse.Result.Token;
                _shareTokenCache[content.Id] = shareToken;
            }

            var (position, rotation) = ComputeSpawnPose();

            InstantiateHandler(handlerPrefab.gameObject, shareToken, SceneManager.GetActiveScene(), position, rotation);
        }

        private (Vector3 position, Quaternion rotation) ComputeSpawnPose()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[RemoteContentManager] No main camera, spawning at origin");
                return (Vector3.zero, Quaternion.identity);
            }

            var origin = mainCamera.transform;
            var forward = origin.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude < 0.001f ? Vector3.forward : forward.normalized;
            var position = origin.position + forward * _SpawnDistance + Vector3.up * _SpawnHeight;
            var rotation = Quaternion.LookRotation(-forward, Vector3.up);
            return (position, rotation);
        }

        private void InstantiateHandler(GameObject handlerPrefab, string shareToken, Scene scene, Vector3 position, Quaternion rotation)
        {
#if FISHNET
            var networkManager = InstanceFinder.NetworkManager;
            var newNob = networkManager.GetPooledInstantiated(handlerPrefab, position, rotation, false);
            networkManager.ServerManager.Spawn(newNob, networkManager.ClientManager.Connection, scene);
            
            var handler = newNob.GetComponent<ARemoteContentHandler>();
            if (handler == null) 
            {
                Debug.LogError($"[RemoteContentManager] Spawned prefab missing ARemoteContentHandler");
                return;
            }
            handler.SetShareToken(shareToken);
#else
            var handler = Instantiate(handlerPrefab, position, rotation);
            if (handler.TryGetComponent<ARemoteContentHandler>(out var remoteContentHandler))
            {
                remoteContentHandler.SetShareToken(shareToken);
            }
#endif
        }

        public void RequestHandlerSelected(ARemoteContentHandler handler)
        {
            if (FocusedHandler == handler)
                return;
            
            FocusedHandler = handler;
            try
            {
                FocusedHandlerChanged?.Invoke(handler);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void DespawnHandler(ARemoteContentHandler handler)
        {
            if (FocusedHandler == handler)
            {
                FocusedHandler = null;
                try
                {
                    FocusedHandlerChanged?.Invoke(null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            
#if FISHNET
            var networkManager = InstanceFinder.NetworkManager;
            networkManager.ServerManager.Despawn(handler.gameObject);
#else
            Destroy(handler.gameObject);
#endif
        }
    }
}
