// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent
{
    public class RemoteContentManager : ALoadableServiceBehaviour<RemoteContentManager>
    {
        [SerializeField]
        private List<ARemoteContentHandler> _HandlerPrefabs = new();
        
        public void SpawnHandler(ContentItemInfo content)
        {
            if (!_HandlerPrefabs.TryGetFirst(r => r.IsValidFor(content), out var handlerPrefab))
            {
                Debug.LogError($"[RemoteContentManager] No handler found for media type '{content.MediaTypeKey}'");
                return;
            }
            
            var handler = Instantiate(handlerPrefab);
            handler.Initialize(content);
        }

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            return UniTask.CompletedTask;
        }

        protected override void PopulateDependencies(HashSet<Type> dependencies)
        {
            base.PopulateDependencies(dependencies);
            dependencies.Add(typeof(ServiceHubManager));
        }
    }
}
