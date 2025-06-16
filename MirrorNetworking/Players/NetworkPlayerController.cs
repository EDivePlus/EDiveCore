// Author: František Holubec
// Created: 23.04.2025

using EDIVE.AssetTranslation;
using EDIVE.Avatars.Scripts;
using EDIVE.StateHandling.ToggleStates;
using Mirror;
using UnityEngine;
using UVRN.Player;

namespace EDIVE.MirrorNetworking.Players
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private AToggleState _LocalPlayerToggle;

        [SerializeField]
        private Transform _AvatarRoot;

        [SerializeField]
        private IKTargetAssigner _IKAssigner;

        [SyncVar]
        private Color _color = Color.white;

        [SyncVar]
        private string _username;

        [SyncVar]
        private string _role;

        [SyncVar]
        private int _connectionID = -1;

        [SyncVar(hook = nameof(HandleAvatarChanged))]
        private string _avatarID;

        private GameObject _avatarInstance;

        public Transform AvatarRoot => _AvatarRoot;

        public string Username => _username;
        public string Role => _role;
        public Color Color => _color;
        public int ConnectionID => _connectionID;
        public string AvatarID => _avatarID;

        public override void OnStartClient()
        {
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(isLocalPlayer);
        }

        [Server]
        public void ApplyProfile(NetworkConnectionToClient conn, PlayerProfile profile, int connectionId)
        {
            _username = profile.username;
            _role = profile.role;
            _color = profile.color;
            _connectionID = connectionId;
            ApplyAvatar(profile.avatarId, conn);
        }

        [Server]
        private void ApplyAvatar(string avatarId, NetworkConnectionToClient conn)
        {
            if (string.IsNullOrEmpty(avatarId))
                return;

            _avatarID = avatarId;
        }

        [Command]
        public void CmdSetAvatar(string avatarId, NetworkConnectionToClient sender = null)
        {
            ApplyAvatar(avatarId, sender);
        }

        public override void OnStartLocalPlayer()
        {
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(true);

            if (_IKAssigner != null && _avatarInstance != null)
                _IKAssigner.Assign(_avatarInstance);
        }

        public void HandleAvatarChanged(string oldValue, string newValue)
        {
            CreateLocalAvatar(newValue);
        }

        private void CreateLocalAvatar(string avatarId)
        {
            if (_avatarInstance || string.IsNullOrEmpty(avatarId))
                return;

            if (!DefinitionTranslationUtils.TryGetDefinition<AvatarDefinition>(avatarId, out var def) || !def.IsValid())
            {
                Debug.LogError($"Invalid avatar ID {avatarId}");
                return;
            }

            _avatarInstance = Instantiate(def.AvatarPrefab, _AvatarRoot, false);
            _avatarInstance.name = def.AvatarPrefab.name;

            if (_IKAssigner != null)
                _IKAssigner.Assign(_avatarInstance);
        }
    }
}
