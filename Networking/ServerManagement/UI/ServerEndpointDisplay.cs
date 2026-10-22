// Author: František Holubec
// Created: 29.04.2026

using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.View.Tooltips;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using TMPro;
using UnityEngine;

namespace EDIVE.Networking.ServerManagement.UI
{
    public class ServerEndpointDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _NameText;
        
        [SerializeField]
        private TMP_Text _EndpointText;
        
        [SerializeField]
        private AToggleState _IsActiveState;

        [Tooltip("Longer endpoints keep the start and end, full text in the tooltip")]
        [SerializeField]
        private int _MaxEndpointLength = 20;

        [SerializeField]
        private TooltipTrigger _EndpointTooltip;

        [SerializeField]
        private StringVisualID _TooltipID;
        
        private AServerEndpoint _serverEndpoint;
        private NetworkServerManager _serverManager;

        private void OnEnable()
        {
            if (AppCore.Services.TryGet<NetworkServerManager>(out var serverManager))
            {
                _serverManager = serverManager;
                _serverManager.ConnectedEndpointChanged += OnConnectedEndpointChanged;
            }
        }

        private void OnDisable()
        {
            if (_serverManager != null)
            {
                _serverManager.ConnectedEndpointChanged -= OnConnectedEndpointChanged;
                _serverManager = null;
            }
        }

        public void SetEndpoint(AServerEndpoint serverEndpoint)
        {
            if (serverEndpoint == null)
                return;
            
            _serverEndpoint = serverEndpoint;
            SetActive(_serverManager != null && ReferenceEquals(_serverManager.ConnectedEndpoint, serverEndpoint));
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (_serverEndpoint == null)
                return;

            if (_NameText)
                _NameText.text = _serverEndpoint.Name;
            
            var endpoint = _serverEndpoint.EndpointText ?? string.Empty;
            if (_EndpointText)
                _EndpointText.text = Shorten(endpoint);

            if (_EndpointTooltip != null && _TooltipID != null)
                _EndpointTooltip.SetPreset(new VisualPreset(new StringVisualPresetRecord(_TooltipID, endpoint)));
        }

        private string Shorten(string text)
        {
            if (_MaxEndpointLength <= 0 || text.Length <= _MaxEndpointLength)
                return text;

            var head = _MaxEndpointLength / 2;
            var tail = _MaxEndpointLength - 1 - head;
            return $"{text[..head]}…{text[^tail..]}";
        }
        
        public void SetActive(bool active)
        {
            if (_IsActiveState) _IsActiveState.SetState(active);
        }

        private void OnConnectedEndpointChanged(AServerEndpoint endpoint)
        {
            var isActive = _serverManager != null && _serverEndpoint != null && ReferenceEquals(endpoint, _serverEndpoint);
            SetActive(isActive);
        }
    }
}
