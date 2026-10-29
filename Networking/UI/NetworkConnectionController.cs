// Author: Michal Petr
// Created: 19.05.2026

using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using EDIVE.Configuration;
using EDIVE.Core;
using EDIVE.Localization;
using EDIVE.Networking.ServerManagement;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.SerializableDictionary;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
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
        [EnhancedBoxGroup("Join errors", SpaceBefore = 8)]
        [Tooltip("Message under the join field")]
        private LocalizeStringEvent _JoinErrorLocalize;

        [SerializeField]
        [EnhancedBoxGroup("Join errors")]
        [Tooltip("On while a join error is shown")]
        private AToggleState _JoinErrorState;

        [SerializeField]
        [EnhancedBoxGroup("Join errors")]
        [Tooltip("Unknown is used for errors without their own message")]
        private SerializableDictionary<JoinError, SafeLocalizedString> _JoinErrorMessages = new();

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

        private bool _joining;

        private void OnEnable()
        {
            if (_DirectJoinButton) _DirectJoinButton.onClick.AddListener(OnDirectJoinClicked);
            if (_DirectJoinInput) _DirectJoinInput.onValueChanged.AddListener(OnDirectJoinInputChanged);
            ShowJoinError(JoinError.None);

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
            if (_DirectJoinInput) _DirectJoinInput.onValueChanged.RemoveListener(OnDirectJoinInputChanged);
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
        
        private void OnDirectJoinInputChanged(string value) => ShowJoinError(JoinError.None);

        private void OnDirectJoinClicked()
        {
            if (_DirectJoinInput == null || _joining)
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
                Debug.LogWarning($"[NetworkConnectionController] '{input}' is not a valid IP:port address or lobby code.");
                ShowJoinError(JoinError.InvalidInput);
                return;
            }

            JoinAsync(joinRequest).Forget();
        }

        private async UniTaskVoid JoinAsync(IJoinRequest joinRequest)
        {
            _joining = true;
            ShowJoinError(JoinError.None);
            if (_DirectJoinButton) _DirectJoinButton.interactable = false;
            var (canceled, error) = await AppCore.Services.Get<NetworkServerManager>()
                .ConnectToServerAsync(joinRequest, destroyCancellationToken)
                .SuppressCancellationThrow();
            if (!this)
                return;
            _joining = false;
            if (_DirectJoinButton) _DirectJoinButton.interactable = true;
            if (!canceled)
                ShowJoinError(error);
        }

        private void ShowJoinError(JoinError error)
        {
            SafeLocalizedString message = null;
            var shown = error is not (JoinError.None or JoinError.Busy)
                && (_JoinErrorMessages.TryGetValue(error, out message) || _JoinErrorMessages.TryGetValue(JoinError.Unknown, out message))
                && message is { IsEmpty: false };
            if (shown && _JoinErrorLocalize)
                _JoinErrorLocalize.StringReference = message;
            if (_JoinErrorState) _JoinErrorState.SetState(shown);
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
                if (!IsFullAddress(hostPart) || !ushort.TryParse(portPart, out port))
                    return false;

                address = hostPart;
                return true;
            }

            if (!IsFullAddress(input))
                return false;

            address = input;
            return true;
        }

        // IPAddress also takes short forms like 192.168.1
        private static bool IsFullAddress(string input) =>
            IPAddress.TryParse(input, out var address) && (address.AddressFamily == AddressFamily.InterNetworkV6 || input.Split('.').Length == 4);

        private static bool IsLobbyCode(string input)
        {
            return input.AsValueEnumerable().All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9');
        }
    }
}
