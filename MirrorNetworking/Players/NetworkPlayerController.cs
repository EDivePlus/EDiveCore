// Author: František Holubec
// Created: 23.04.2025

using EDIVE.AssetTranslation;
using EDIVE.Avatars.Scripts;
using EDIVE.Core;
using EDIVE.DataStructures.ScriptableVariables;
using EDIVE.StateHandling.ToggleStates;
using Mirror;
using UnityEngine;
using UVRN.Player;
using EDIVE.XRTools.Controls;

namespace EDIVE.MirrorNetworking.Players
{
    /// <summary>
    /// Class responsible for the player model networking.
    /// </summary>
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private AToggleState _LocalPlayerToggle;

        [SerializeField]
        private Transform _AvatarRoot;

        private GameObject _avatarInstance;

        [SyncVar]
        private Color _color = Color.white;

        [SyncVar]
        private string _username;

        [SyncVar]
        private string _role;

        [SyncVar]
        private int _connectionID = -1;

        [SyncVar]
        private string _avatarID;

        public string Username => _username;
        public string Role => _role;
        public Color Color => _color;
        public int ConnectionID => _connectionID;
        public string AvatarID => _avatarID;

        public override void OnStartClient()
        {
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(isLocalPlayer);
            //Client_ConnectedPlayers[id] = this;
            //Client_OnPlayerJoined.Invoke(id);
        }

        public override void OnStopClient()
        {
            //Client_ConnectedPlayers.Remove(id);
            // Client_OnPlayerLeft.Invoke(id);
        }

        // Done according to https://mirror-networking.gitbook.io/docs/guides/gameobjects/custom-character-spawning
        // It would be nicer to use something instead of all the syncvars, but this is the best for now
        [Server]
        public void ApplyProfile(PlayerProfile profile, int connectionId)
        {
            _username = profile.username;
            _role = profile.role;
            _color = profile.color;
            _connectionID = connectionId;
            ApplyAvatar(profile.avatarId);
        }

        [Server]
        private void ApplyAvatar(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId))
                return;

            _avatarID = avatarId;

            if (!_AvatarRoot)
                return;

            if (!DefinitionTranslationUtils.TryGetDefinition<AvatarDefinition>(_avatarID, out var def) || !def.IsValid())
            {
                Debug.LogError($"AvatarDefinition not found or invalid for ID: {_avatarID}");
                return;
            }

            if (_avatarInstance)
                Destroy(_avatarInstance);

            _avatarInstance = Instantiate(def.AvatarPrefab, _AvatarRoot, false);
            _avatarInstance.name = def.AvatarPrefab.name;
            Debug.Log($"Avatar {_avatarID} set for player '{_username}'");

            //var vis = _avatarInstance.AddComponent<SelfVisibility>();
        }

        [Command]
        public void CmdSetAvatar(string avatarId)
        {
            ApplyAvatar(avatarId);
        }

        public override void OnStartLocalPlayer()
        {
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(true);
        }
    }
}
