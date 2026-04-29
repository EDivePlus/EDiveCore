// Author: František Holubec
// Created: 29.04.2026

using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.XRTools.Utils
{
    public class ToggleOnActivate : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _Activation;
        
        [SerializeField]
        private AToggleState _ToggleState;

        private void OnEnable()
        {
            _Activation?.RegisterActivationListener(Toggle);
        }

        private void OnDisable()
        {
            _Activation?.UnregisterActivationListener(Toggle);
        }
        
        [Button]
        private void Toggle()
        {
            _ToggleState.State = !_ToggleState.State;
        }
    }
}
