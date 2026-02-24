// Author: František Holubec
// Created: 21.07.2025

using System;
using System.Collections.Generic;
using EDIVE.AssetTranslation;
using EDIVE.NativeUtils;
using EDIVE.Replay.Strategies;
using UnityEngine;

namespace EDIVE.Replay.Agents
{
    public class ReplayAgentDefinition : AUniqueDefinition, ISerializationCallbackReceiver
    {
        [SerializeReference]
        private List<AReplayAgentSpawnStrategy> _SpawnStrategies;
        
        [Obsolete("Use spawn strategies instead")]
        [HideInInspector]
        [SerializeField]
        private ReplayAgentHandler _Prefab;
        
        public bool TryGetStrategy<TStrategy>(out TStrategy strategy) where TStrategy : AReplayAgentSpawnStrategy
        {
            return _SpawnStrategies.TryGetFirstT(out strategy);
        }

    #region Migration
        [Obsolete]
        public void OnBeforeSerialize() => MigrateToStrategies();

        [Obsolete]
        public void OnAfterDeserialize() => MigrateToStrategies();

        [Obsolete]
        private void MigrateToStrategies()
        {
            if (_SpawnStrategies != null && _SpawnStrategies.Count > 0)
                return;
            
            _SpawnStrategies ??= new List<AReplayAgentSpawnStrategy>();
            if (_Prefab != null) 
                _SpawnStrategies.Add(new PrefabReplayAgentSpawnStrategy(_Prefab));
        }
    #endregion
    }
}
