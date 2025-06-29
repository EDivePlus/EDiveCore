// Author: František Holubec
// Created: 29.06.2025

using EDIVE.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.MirrorNetworking.UI
{
    [RequireComponent(typeof(Button))]
    public class StartNetworkRuntimeButton : MonoBehaviour
    {
        [SerializeField]
        private NetworkRuntimeMode _NetworkMode = NetworkRuntimeMode.Client;
        
        private Button _button;
        
        private void Awake()
        {
            if (TryGetComponent(out _button))
                _button.onClick.AddListener(OnButtonClicked);
        }
        
        private void OnDestroy()
        {
            if (_button != null) 
                _button.onClick.RemoveListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            AppCore.Services.Get<MasterNetworkManager>().StartRuntime(_NetworkMode);
        }
    }
}
