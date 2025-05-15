// Author: František Holubec
// Created: 23.04.2025
using EDIVE.AssetTranslation;
using EDIVE.Avatars.Scripts;
using EDIVE.Core;
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
                public override void OnStartLocalPlayer()
        {
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(true);

            SpawnAvatarFromProfile();
        }

        private void SpawnAvatarFromProfile()
        {
            var profile = PlayerProfileManager.LOCAL_PROFILE;

            if (string.IsNullOrEmpty(profile.avatarId))
            {
                Debug.LogError("Avatar ID is missing in profile.");
                return;
            }

            if (_AvatarRoot == null)
            {
                Debug.LogError("AvatarRoot is not set on NetworkPlayerController.");
                return;
            }

            if (DefinitionTranslationUtils.TryGetDefinition<AvatarDefinition>(profile.avatarId, out var def) && def.IsValid())
            {
                if (_avatarInstance != null)
                    Destroy(_avatarInstance);

                _avatarInstance = Instantiate(def.AvatarPrefab, _AvatarRoot, false);
                _avatarInstance.name = def.AvatarPrefab.name;

                Debug.Log($"[Avatar] '{_avatarInstance.name}' instantiated successfully under AvatarRoot.");

                if (isLocalPlayer)
                    TryAssignIKTargets(_avatarInstance);
            }
            else
            {
                Debug.LogError($"AvatarDefinition not found or invalid for ID: {profile.avatarId}");
            }
        }


        private void TryAssignIKTargets(GameObject avatarInstance)
        {
            var ikRig = avatarInstance.GetComponentInChildren<IKTargetFollowVRRig>();
            if (ikRig == null)
            {
                Debug.LogWarning("IKTargetFollowVRRig not found in avatar prefab.");
                return;
            }

            var controls = AppCore.Services.Get<ControlsManager>().Controls;
            ikRig.head.vrTarget = controls.HeadTargetIK;
            ikRig.leftHand.vrTarget = controls.LeftHandTargetIK;
            ikRig.rightHand.vrTarget = controls.RightHandTargetIK;


            Debug.Log("IK rig successfully assigned to XR targets.");
        }
    }
}
