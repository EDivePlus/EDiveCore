// Author: František Holubec
// Created: 03.03.2026

using System;
using EDIVE.Utils.Activations;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.StagePlay
{
    public class StagePlaySelector : MonoBehaviour
    {
        [FormerlySerializedAs("_DefaultDefinition")]
        [SerializeField]
        private StagePlayDefinition _Definition;
        
        [SerializeReference]
        private IActivation _Activation;

        public StagePlayDefinition Definition
        {
            get => _Definition;
            set => _Definition = value;
        }

        public event Action<StagePlayDefinition> DefinitionSelected;
        
        private void OnEnable()
        {
            _Activation.RegisterActivationListener(OnActivated);
        }

        private void OnDisable()
        {
            _Activation.UnregisterActivationListener(OnActivated);
        }
        
        private void OnActivated()
        {
            if (Definition != null)
                DefinitionSelected?.Invoke(Definition);
        }
    }
}
