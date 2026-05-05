// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Networking;
using EDIVE.ServiceHub.RemoteContent.Handlers;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.SceneManagement;
using Channel = FishNet.Transporting.Channel;

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

        private NetworkManager _networkManager;

        public void SpawnHandler(ContentItemInfo content)
        {
            if (content == null || string.IsNullOrEmpty(content.Id))
            {
                Debug.LogError("[RemoteContentManager] SpawnHandler called with null or invalid content");
                return;
            }

            var prefabIndex = _HandlerPrefabs.FindIndex(r => r != null && r.IsValidFor(content));
            if (prefabIndex < 0)
            {
                Debug.LogError($"[RemoteContentManager] No handler found for media type '{content.MediaTypeKey}'");
                return;
            }

            if (_networkManager == null || _networkManager.ClientManager == null || !_networkManager.ClientManager.Started)
            {
                Debug.LogError("[RemoteContentManager] Cannot spawn — client not connected");
                return;
            }

            var (position, rotation) = ComputeSpawnPose();
            var msg = new RemoteContentSpawnRequestMessage
            {
                PrefabIndex = prefabIndex,
                ContentId = content.Id,
                SceneName = SceneManager.GetActiveScene().name,
                Position = position,
                Rotation = rotation,
            };
            _networkManager.ClientManager.Broadcast(msg);
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

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            _networkManager = InstanceFinder.NetworkManager;
            if (_networkManager == null)
                return UniTask.CompletedTask;

            _networkManager.ServerManager.RegisterBroadcast<RemoteContentSpawnRequestMessage>(OnServerSpawnRequest);
            return UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_networkManager != null && _networkManager.ServerManager != null)
            {
                _networkManager.ServerManager.UnregisterBroadcast<RemoteContentSpawnRequestMessage>(OnServerSpawnRequest);
            }
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(ServiceHubManager));
            dependencies.Add(typeof(MasterNetworkManager));
        }

        private void OnServerSpawnRequest(NetworkConnection conn, RemoteContentSpawnRequestMessage msg, Channel channel)
        {
            if (msg.PrefabIndex < 0 || msg.PrefabIndex >= _HandlerPrefabs.Count)
            {
                Debug.LogError($"[RemoteContentManager] Invalid prefab index {msg.PrefabIndex} from client {conn.ClientId}");
                return;
            }
            if (string.IsNullOrEmpty(msg.ContentId))
            {
                Debug.LogError($"[RemoteContentManager] Empty content id from client {conn.ClientId}");
                return;
            }

            var prefab = _HandlerPrefabs[msg.PrefabIndex];
            if (prefab == null)
            {
                Debug.LogError($"[RemoteContentManager] Prefab at index {msg.PrefabIndex} is null");
                return;
            }

            var targetScene = conn.Scenes.FirstOrDefault(s => s.IsValid() && s.name == msg.SceneName);
            if (!targetScene.IsValid())
            {
                Debug.LogError($"[RemoteContentManager] Client {conn.ClientId} not in scene '{msg.SceneName}', cannot spawn");
                return;
            }

            var netObj = _networkManager.GetPooledInstantiated(prefab.gameObject, msg.Position, msg.Rotation, true);
            _networkManager.ServerManager.Spawn(netObj, conn, targetScene);

            var handler = netObj.GetComponent<ARemoteContentHandler>();
            handler.ServerSetContentId(msg.ContentId);
        }
    }
}
