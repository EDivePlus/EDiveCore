// Author: František Holubec
// Created: 03.07.2025

using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay.Agents
{
    // MonoBehaviour handler for ReplayAgent, separation required because the object can get destroyed.
    public class ReplayAgentHandler : MonoBehaviour
    {
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private ReplayAgent _Agent;
        public ReplayAgent Agent => _Agent;
        
        private bool _initialized;

        private void Awake()
        {
            // Delay initialization to end of frame so we can inject dependencies properly. Only necessary in Awake.
            UniTask.Void(async cancellationToken =>
            {
                await UniTask.Yield(cancellationToken);
                InitializeAgent();
            }, destroyCancellationToken);
        }

        private void OnDestroy()
        {
            _Agent.Terminate();
        }
        
        private void InitializeAgent()
        {
            if (_initialized) 
                return;
            
            _Agent.Initialize(this);
            _initialized = true;
        }
    }
}
