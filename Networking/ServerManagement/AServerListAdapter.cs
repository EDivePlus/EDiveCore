// Author: František Holubec
// Created: 15.07.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.External.Signals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement
{
    public abstract class AServerListAdapter : MonoBehaviour
    {
        public Dictionary<string, ServerRecord> Servers { get; } = new();
        public Signal ServerListUpdated { get; } = new();

        [HideReferenceObjectPicker]
        [ShowInInspector]   
        [EnableGUI]
        private readonly List<ServerRecord> _serverList = new();
        
        protected ServerRecord _currentServerRecord;
        protected ServerConfig _serverConfig;

        public async UniTask Initialize(ServerConfig serverConfig)
        {
            _serverConfig = serverConfig;
            await Initialize();
        }

        public virtual UniTask Initialize() => UniTask.CompletedTask;
        
        public virtual void PopulateDependencies(HashSet<Type> dependencies) { }
        
        public virtual UniTask PrepareServerStart()
        {
            StopSearch();
            return UniTask.CompletedTask;
        }

        public virtual void StartServer(){}
        public virtual void StopServer(){}

        public virtual void StartSearch(){}
        public virtual void StopSearch(){}

        public virtual void RegisterHostServer(ServerRecord hostServer)
        {
            _currentServerRecord = hostServer;
        }
        
        public abstract UniTask<(bool, ServerRecord)> TryHandleJoinRequest(IJoinRequest request);

        protected void SetServers(IEnumerable<ServerRecord> records)
        {
            Servers.Clear();
            _serverList.Clear();
            _serverList.AddRange(records.Where(r => r != null && !string.IsNullOrEmpty(r.InstanceID)));
            
            var now = DateTime.UtcNow;
            foreach (var record in _serverList)
            {
                record.LastUpdated = now;
                Servers[record.InstanceID] = record;
            }
            ServerListUpdated.Dispatch();
        }
    }
}
