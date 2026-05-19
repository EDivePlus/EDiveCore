// Author: Michal Petr
// Created: 19.05.2026

using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using EDIVE.Configuration;
using EDIVE.Core;
using EDIVE.Networking.ServerManagement;
using EDIVE.Networking.ServerManagement.PurrNet;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using ZLinq;

namespace EDIVE.Networking.UI
{
    public class NetworkConnectionController :  MonoBehaviour
    {
        [SerializeField]
        private ServerConfig _Config;
        
        [SerializeField]
        [PropertySpace]
        private TMP_InputField _DirectJoinInput;

        [SerializeField]
        private Button _DirectJoinButton;
        
        [SerializeField]
        [PropertySpace]
        private Button _HostButton;
        
        [SerializeField]
        [EnhancedBoxGroup("Host Config", SpaceBefore = 8)]
        private TMP_InputField _ServerNameInput;
        
        [SerializeField]
        [EnhancedBoxGroup("Host Config")]
        private TMP_InputField _MaxPlayersInput;
        
        [SerializeField]
        [EnhancedBoxGroup("Host Config")]
        private TMP_InputField _PublicAddressInput;
        
        [SerializeField]
        [EnhancedBoxGroup("Host Config")]
        private TMP_InputField _PortInput;
        
        [SerializeField]
        [EnhancedBoxGroup("Host Config")]
        private Toggle _IsPrivateToggle;
        
        [SerializeField]
        [EnhancedBoxGroup("Host Config")]
        private TMP_InputField _ServiceHubIDInput;
        
        [SerializeField]
        [EnhancedBoxGroup("Host Config")]
        private TMP_InputField _ServiceHubSecretInput;

        private void OnEnable()
        {
            if (_DirectJoinButton) _DirectJoinButton.onClick.AddListener(OnDirectJoinClicked);
            if (_HostButton) _HostButton.onClick.AddListener(OnHostClicked);
            
            if (_ServerNameInput) _ServerNameInput.text = _Config.ServerName;
            if (_MaxPlayersInput) _MaxPlayersInput.text = _Config.MaxPlayers.ToString();
            if (_PublicAddressInput) _PublicAddressInput.text = _Config.PublicAddress;
            if (_PortInput) _PortInput.text = _Config.Port.ToString();
            if (_IsPrivateToggle) _IsPrivateToggle.isOn = _Config.IsPrivate;
            if (_ServiceHubIDInput) _ServiceHubIDInput.text = _Config.ServerID;
            if (_ServiceHubSecretInput) _ServiceHubSecretInput.text = _Config.ServerSecret;
            
            if (_ServerNameInput) _ServerNameInput.onValueChanged.AddListener(OnInputChanged);
            if (_MaxPlayersInput) _MaxPlayersInput.onValueChanged.AddListener(OnInputChanged);
            if (_PublicAddressInput) _PublicAddressInput.onValueChanged.AddListener(OnInputChanged);
            if (_PortInput) _PortInput.onValueChanged.AddListener(OnInputChanged);
            if (_IsPrivateToggle) _IsPrivateToggle.onValueChanged.AddListener(OnToggleChanged);
            if (_ServiceHubIDInput) _ServiceHubIDInput.onValueChanged.AddListener(OnInputChanged);
            if (_ServiceHubSecretInput) _ServiceHubSecretInput.onValueChanged.AddListener(OnInputChanged);
        }

        private void OnDisable()
        {
            if (_ServerNameInput) _ServerNameInput.onValueChanged.RemoveListener(OnInputChanged);
            if (_MaxPlayersInput) _MaxPlayersInput.onValueChanged.RemoveListener(OnInputChanged);
            if (_PublicAddressInput) _PublicAddressInput.onValueChanged.RemoveListener(OnInputChanged);
            if (_PortInput) _PortInput.onValueChanged.RemoveListener(OnInputChanged);
            if (_IsPrivateToggle) _IsPrivateToggle.onValueChanged.RemoveListener(OnToggleChanged);
            if (_ServiceHubIDInput) _ServiceHubIDInput.onValueChanged.RemoveListener(OnInputChanged);
            if (_ServiceHubSecretInput) _ServiceHubSecretInput.onValueChanged.RemoveListener(OnInputChanged);
            
            if (_DirectJoinButton) _DirectJoinButton.onClick.RemoveListener(OnDirectJoinClicked);
            if (_HostButton) _HostButton.onClick.RemoveListener(OnHostClicked);
        }

        private void OnInputChanged(string value) => SaveConfig();
        private void OnToggleChanged(bool value) => SaveConfig();

        private void SaveConfig()
        {
            if (_Config == null) return;
            
            if (_ServerNameInput) _Config.ServerName = _ServerNameInput.text;
            if (_MaxPlayersInput && int.TryParse(_MaxPlayersInput.text, out var maxPlayers)) _Config.MaxPlayers = maxPlayers;
            if (_PublicAddressInput) _Config.PublicAddress = _PublicAddressInput.text;
            if (_PortInput && ushort.TryParse(_PortInput.text, out var port)) _Config.Port = port;
            if (_IsPrivateToggle) _Config.IsPrivate = _IsPrivateToggle.isOn;
            if (_ServiceHubIDInput) _Config.ServerID = _ServiceHubIDInput.text;
            if (_ServiceHubSecretInput) _Config.ServerSecret = _ServiceHubSecretInput.text;
            
            AppCore.Services.Get<LocalConfigLoader>().SaveConfig(_Config).Forget();
        }
        
        private void OnDirectJoinClicked()
        {
            if (_DirectJoinInput == null)
                return;

            var input = _DirectJoinInput.text?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                Debug.LogWarning("[NetworkConnectionController] Direct join input is empty.");
                return;
            }

            var joinRequest = ParseJoinRequest(input);
            if (joinRequest == null)
            {
                Debug.LogError($"[NetworkConnectionController] '{input}' is not a valid IP:port address or lobby code.");
                return;
            }
                
            AppCore.Services.Get<NetworkServerManager>().ConnectToServer(joinRequest);
        }
        
        private void OnHostClicked()
        {
            AppCore.Services.Get<MasterNetworkManager>().StartRuntime(NetworkRuntimeMode.Host);
        }

        private IJoinRequest ParseJoinRequest(string input)
        {
            if (TryParseAddress(input, out var address, out var port))
            {
                return new DirectJoinRequest(address, port);
            }

            if (IsLobbyCode(input))
            {
                return new CodeJoinRequest(input);
            }

            return null;
        }
        
        private static bool TryParseAddress(string input, out string address, out ushort port)
        {
            address = null;
            port = 0;

            var separatorIndex = input.LastIndexOf(':');
            if (separatorIndex >= 0)
            {
                var hostPart = input[..separatorIndex];
                var portPart = input[(separatorIndex + 1)..];
                if (!IPAddress.TryParse(hostPart, out _) || !ushort.TryParse(portPart, out port))
                    return false;

                address = hostPart;
                return true;
            }

            if (!IPAddress.TryParse(input, out _))
                return false;

            address = input;
            return true;
        }

        private static bool IsLobbyCode(string input)
        {
            return input.AsValueEnumerable().All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9');
        }
    }
}
