// Author: František Holubec
// Created: 03.09.2025

using EDIVE.Core;
using EDIVE.ScriptableArchitecture.Variables;
using EDIVE.Utils.Activations;
using UnityEngine;

namespace EDIVE.XRTools.Controls
{
    public class TeleportButton : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _Activation;
        
        [SerializeField]
        private ScriptableVariableField<Transform> _Location;
        
        private void Awake()
        {
            _Activation?.RegisterActivationListener(OnActivated);
        }
        
        private void OnDestroy()
        {
            _Activation?.UnregisterActivationListener(OnActivated);
        }

        private void OnActivated()
        {
            var location = _Location.Value;
            if (location == null)
                return;
            
            if (AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
            {
                controlsManager.RequestTeleport(location.position, location.rotation);
            }
        }
    }
}
