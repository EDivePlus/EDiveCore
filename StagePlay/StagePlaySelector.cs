// Author: František Holubec
// Created: 03.03.2026

using EDIVE.Utils.Activations;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace EDIVE.StagePlay
{
    public class StagePlaySelector : NetworkBehaviour
    {
        [SerializeField]
        private StagePlayDefinition _DefaultDefinition;

        [SerializeField]
        private StagePlayController _Controller;

        [SerializeReference]
        private IActivation _Activation;

        private readonly SyncVar<StagePlayDefinition> _currentDefinition = new();

        private void OnEnable()
        {
            _Activation.RegisterActivationListener(OnActivated);
        }

        private void OnDisable()
        {
            _Activation.UnregisterActivationListener(OnActivated);
        }

        [ServerRpc]
        public void SetDefinition(StagePlayDefinition definition)
        {
            _currentDefinition.Value = definition;
        }
        
        private void OnActivated()
        {
            if (_Controller == null || _currentDefinition.Value == null)
                return;
            _Controller.SetDefinition(_currentDefinition.Value);
        }
    }
}
