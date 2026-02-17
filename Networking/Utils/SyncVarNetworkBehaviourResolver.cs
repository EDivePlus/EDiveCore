// Author: František Holubec
// Created: 11.02.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    /// <summary>
    /// Resolves NetworkBehaviour references from a SyncVar&lt;int&gt; ObjectId.
    /// SyncVars cannot be nested - declare SyncVar&lt;int&gt; as direct field, then bind this resolver to it.
    /// </summary>
    public class SyncVarNetworkBehaviourResolver<T> where T : NetworkBehaviour
    {
        public T Value { get; private set; }
        public event Action<T> OnChanged;
        
        private const float TIMEOUT = 5f;
        
        // Store as object to avoid FishNet detecting this as a SyncType field
        private object _syncVarRef;
        
        public void BindToSyncVar(SyncVar<int> syncVar)
        {
            if (_syncVarRef is SyncVar<int> oldSyncVar) 
                oldSyncVar.OnChange -= HandleChange;
            
            _syncVarRef = syncVar;
            syncVar.OnChange += HandleChange;
        }
        
        public void SetValue(T value)
        {
            if (_syncVarRef is not SyncVar<int> syncVar)
            {
                Debug.LogError("[SyncVarNetworkBehaviourResolver] No SyncVar bound!");
                return;
            }
            
            Value = value;
            syncVar.Value = value != null ? value.ObjectId : 0;
        }

        private void HandleChange(int prev, int next, bool asServer)
        {
            if (asServer)
            {
                // On server, Value is already set by SetValue(), just fire the event
                OnChanged?.Invoke(Value);
                return;
            }
            
            // On clients, resolve the NetworkObject asynchronously
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(TIMEOUT));
            ResolveAsync(next, cts.Token).Forget();
        }

        private async UniTaskVoid ResolveAsync(int objectId, CancellationToken cancellationToken)
        {
            if (objectId == 0)
            {
                Value = null;
                OnChanged?.Invoke(null);
                return;
            }

            var result = await WaitForComponent(objectId, cancellationToken);
            Value = result;
            OnChanged?.Invoke(result);
        }

        private async UniTask<T> WaitForComponent(int objectId, CancellationToken cancellationToken)
        {
            if (objectId == 0) 
                return null;

            var networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null) return null;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (networkManager.ClientManager.Objects.Spawned.TryGetValue(objectId, out var netObj))
                    return netObj.GetComponent<T>();

                await UniTask.Yield(cancellationToken);
            }
            return null;
        }
        
        public void Dispose()
        {
            if (_syncVarRef is SyncVar<int> oldSyncVar) 
                oldSyncVar.OnChange -= HandleChange;
            _syncVarRef = null;
            Value = null;
            OnChanged = null;
        }
    }
}
