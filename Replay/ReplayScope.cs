// Author: Radim Holub
// Created: 15.07.2025

using UnityEngine;
using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.Replay.Agents;
using Sirenix.OdinInspector;

namespace EDIVE.Replay
{
    public class ReplayScope : ScriptableObject
    {
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, ListElementLabelName = "ID")]
        [HideReferenceObjectPicker]
        private readonly List<ReplayAgent> _agents = new();
        
        public IReadOnlyList<ReplayAgent> Agents => _agents;
        
        private readonly Dictionary<string, int> _maxDynamicIds = new();

        public event Action<ReplayAgent> AgentRegistered;
        public event Action<ReplayAgent> AgentUnregistered;
        
        public void RegisterAgent(ReplayAgent agent)
        {
            if (_agents.Contains(agent))
                return;

            if (_maxDynamicIds.TryGetValue(agent.BaseID, out var maxID))
                agent.DynamicID = _maxDynamicIds[agent.BaseID] = maxID + 1;
            else
                _maxDynamicIds[agent.BaseID] = 0;

            _agents.Add(agent);
            AgentRegistered?.Invoke(agent);
        }
        
        public void UnregisterAgent(ReplayAgent agent)
        {
            if (!_agents.Remove(agent)) 
                return;
            
            AgentUnregistered?.Invoke(agent);
        }

        public bool TryGetAgent(string id, out ReplayAgent agent)
        {
            return _agents.TryGetFirst(c => c.ID == id, out agent);
        }
    }
}
