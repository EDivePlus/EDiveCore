// Author: František Holubec
// Created: 21.07.2025

using EDIVE.AssetTranslation;
using UnityEngine;

namespace EDIVE.Replay.Agents
{
    public class ReplayAgentDefinition : AUniqueDefinition
    {
        [SerializeField]
        private ReplayAgentHandler _Prefab;

        public ReplayAgentHandler Prefab => _Prefab;
    }
}
