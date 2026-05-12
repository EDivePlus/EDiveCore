// Author: František Holubec
// Created: 11.02.2026

using System;
using PurrNet;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    /// <summary>
    /// Resolves a typed <see cref="NetworkBehaviour"/> reference from a synced
    /// <see cref="NetworkIdentity"/>. Originally written to work around FishNet's
    /// "SyncVars cannot be nested" limitation. PurrNet supports
    /// <c>SyncVar&lt;NetworkBehaviour&gt;</c> / <c>SyncVar&lt;NetworkIdentity&gt;</c> directly,
    /// so for new code prefer a plain <see cref="SyncVar{T}"/>; this helper is kept as a
    /// thin wrapper for components that already depend on its API surface.
    /// </summary>
    public class SyncVarNetworkBehaviourResolver<T> where T : NetworkBehaviour
    {
        public T Value { get; private set; }
        public event Action<T> OnChanged;

        private SyncVar<NetworkIdentity> _syncVar;

        public void BindToSyncVar(SyncVar<NetworkIdentity> syncVar)
        {
            if (_syncVar != null)
                _syncVar.onChanged -= HandleChange;

            _syncVar = syncVar;
            if (_syncVar == null)
                return;

            _syncVar.onChanged += HandleChange;
            // Resolve the current value so consumers see initial state immediately.
            HandleChange(_syncVar.value);
        }

        public void SetValue(T value)
        {
            if (_syncVar == null)
            {
                Debug.LogError("[SyncVarNetworkBehaviourResolver] No SyncVar bound!");
                return;
            }

            Value = value;
            _syncVar.value = value; // NetworkBehaviour : NetworkIdentity, so the assignment is allowed.
        }

        private void HandleChange(NetworkIdentity identity)
        {
            Value = identity != null ? identity.GetComponent<T>() : null;
            OnChanged?.Invoke(Value);
        }

        public void Dispose()
        {
            if (_syncVar != null)
                _syncVar.onChanged -= HandleChange;
            _syncVar = null;
            Value = null;
            OnChanged = null;
        }
    }
}
