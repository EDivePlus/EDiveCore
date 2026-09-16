// Author: František Holubec
// Created: 16.09.2026

using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Networking.ServerManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Networking.UI
{
    [RequireComponent(typeof(Button))]
    public class ReconnectServerButton : MonoBehaviour
    {
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
            AppCore.Services.Get<NetworkServerManager>().ReconnectAsync(destroyCancellationToken).Forget();
        }
    }
}
