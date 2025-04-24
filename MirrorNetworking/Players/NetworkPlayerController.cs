// Author: František Holubec
// Created: 23.04.2025

using EDIVE.StateHandling.ToggleStates;
using Mirror;
using UnityEngine;
using UVRN.Player;

namespace EDIVE.MirrorNetworking.Players
{
    /// <summary>
    /// Class responsible for the player model networking.
    /// </summary>
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private AToggleState _LocalPlayerToggle;

        [SyncVar(hook = nameof(HandleColorChanged))]
        private Color _color = Color.white;

        [SyncVar(hook = nameof(HandleUsernameChanged))]
        private string _username;

        [SyncVar(hook = nameof(HandleRoleChanged))]
        private string _role;

        [SyncVar(hook = nameof(HandleVisibleChanged))]
        private bool _modelVisible = true;

        [SyncVar(hook = nameof(HandleConnectionIDChanged))]
        private int _connectionID = -1;

        public string Username => _username;
        public string Role => _role;
        public Color Color => _color;
        public int ConnectionID => _connectionID;

        public void HandleUsernameChanged(string oldValue, string newValue)
        {
            _username = newValue;
        }

        public void HandleRoleChanged(string oldValue, string newValue)
        {
            _role = newValue;
        }

        public void HandleVisibleChanged(bool oldValue, bool newValue)
        {
            _modelVisible = newValue;
        }

        public void HandleColorChanged(Color oldValue, Color newValue)
        {
            _color = newValue;
        }

        public void HandleConnectionIDChanged(int oldValue, int newValue)
        {
            _connectionID = newValue;
        }

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
            HandleColorChanged(_color, profile.color);
            HandleUsernameChanged(_username, profile.username);
            HandleRoleChanged(_role, profile.role);
            HandleVisibleChanged(_modelVisible, profile.visibleAvatar);
            HandleConnectionIDChanged(_connectionID, connectionId);
        }
    }
}
