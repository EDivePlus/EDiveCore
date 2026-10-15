// Author: František Holubec
// Created: 15.10.2026

using EDIVE.Utils.Activations;
using UnityEngine;

namespace EDIVE.Debugging
{
    // Opens or closes one host, or every host when none is set.
    public class DebugMenuToggleButton : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _Activation;

        [SerializeField]
        private DebugMenuHost _Host;

        private void Awake()
        {
            _Activation?.Register(OnActivated);
        }

        private void OnDestroy()
        {
            _Activation?.Unregister(OnActivated);
        }

        private void OnActivated()
        {
            if (_Host != null)
                _Host.Toggle();
            else
                DebugMenu.Toggle();
        }
    }
}
