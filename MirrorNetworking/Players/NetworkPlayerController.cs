// Author: František Holubec
// Created: 23.04.2025

using EDIVE.AssetTranslation;
using EDIVE.Avatars;
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

        [SyncVar(hook = nameof(HandleColorChanged))]
        private Color _color = Color.white;

        [SyncVar(hook = nameof(HandleUsernameChanged))]
        private string _username;

        [SyncVar(hook = nameof(HandleRoleChanged))]
        private string _role;

        [SyncVar(hook = nameof(HandleConnectionIDChanged))]
        private int _connectionID = -1;

        [SyncVar(hook = nameof(HandleAvatarChanged))]
        private string _avatarID;

        private AvatarController _avatarInstance;

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

            if (_avatarInstance != null)
                _avatarInstance.IsLocalPlayer = isLocalPlayer;

            RefreshGameObjectName();
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
            CreateLocalAvatar(_avatarID);
        }

        [Command]
        private void CmdSetAvatar(string avatarId, NetworkConnectionToClient sender = null)
        {
            ApplyAvatar(avatarId, sender);
        }

        public void SetAvatar(AvatarDefinition avatarDefinition)
        {
            CmdSetAvatar(avatarDefinition.UniqueID);
        }

        public override void OnStartLocalPlayer()
        {
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(true);

            if (_avatarInstance != null)
            {
                _avatarInstance.IsLocalPlayer = true;
                if (_IKAssigner != null)
                    _IKAssigner.Assign(_avatarInstance);
            }

            if (_IKAssigner != null)
                _IKAssigner.InitializeFollow();
        }

        private void RefreshGameObjectName()
        {
            var objName = $"Player '{Username}' ({_connectionID})";
            if (isLocalPlayer) objName += " [Local]";
            gameObject.name = objName;
        }

        public void HandleConnectionIDChanged(int oldValue, int newValue)
        {
            RefreshGameObjectName();
        }

        public void HandleUsernameChanged(string oldValue, string newValue)
        {
            RefreshGameObjectName();
        }

        public void HandleRoleChanged(string oldValue, string newValue)
        {

        }

        public void HandleColorChanged(Color oldValue, Color newValue)
        {

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
            _avatarInstance.gameObject.name = def.AvatarPrefab.name;
            _avatarInstance.IsLocalPlayer = isLocalPlayer;

            if (_IKAssigner != null)
                _IKAssigner.Assign(_avatarInstance);
        }
    }
}
