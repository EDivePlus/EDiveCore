// Author: František Holubec
// Created: 19.05.2025

using System.Linq;
using EDIVE.Core;
using EDIVE.NativeUtils;
using Edive.Networking;
using TMPro;
using UnityEngine;
using UVRN.Player;

namespace EDIVE.MirrorNetworking.Players
{
    public class ServerRoleController : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown _RoleDropdown;

        [SerializeField]
        private TMP_InputField _PasswordInput;

        private NetworkPlayerConfig _playerConfig;
        private NetworkPlayerManager _networkPlayerManager;

        private void Awake()
        {
            _networkPlayerManager = AppCore.Services.Get<NetworkPlayerManager>();

            if (_RoleDropdown)
            {
                _RoleDropdown.ClearOptions();
                _playerConfig = _networkPlayerManager.PlayerConfig;
                _RoleDropdown.AddOptions(_playerConfig.Roles.Select(r => r.Role).ToList());
                _RoleDropdown.onValueChanged.AddListener(OnRoleChanged);

                if (_playerConfig.Roles.TryGetFirst(r => r.Role == _networkPlayerManager.PlayerProfile.role, out var role))
                {
                    _RoleDropdown.SetValueWithoutNotify(_playerConfig.Roles.IndexOf(role));
                }
            }
            _PasswordInput.onValueChanged.AddListener(OnPasswordChanged);
        }

        private void OnPasswordChanged(string value)
        {
            _networkPlayerManager.PlayerProfile.password = value;
        }

        private void OnRoleChanged(int value)
        {
            var role = _playerConfig.Roles[value];
            _networkPlayerManager.PlayerProfile.role = role.Role;
        }
    }
}
