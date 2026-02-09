// Author: František Holubec
// Created: 23.04.2025

using System;
using EDIVE.Avatars;
using EDIVE.Core;
using EDIVE.Networking.UI;
using EDIVE.StateHandling.ToggleStates;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using FishNet.Connection;
using EDIVE.XRTools.Controls;

namespace EDIVE.Networking.Players
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private AToggleState _LocalPlayerToggle;

        [SerializeField]
        private Transform _AvatarRoot;

        [SerializeField]
        private IKTargetAssigner _IKAssigner;

        [SerializeField]
        private BillboardNameTag _NameTag;
        
        public AvatarController AvatarInstance { get; private set; }

        public string Username => _username.Value;
        public string Role => _role.Value;
        public Color Color => _color.Value;
        public AvatarDefinition AvatarDefinition => _avatarDefinition.Value;
        
        public event Action<AvatarController> AvatarInstanceChanged;
        public event Action<AvatarDefinition> AvatarChanged;
        
        private readonly SyncVar<Color> _color = new(Color.white);
        private readonly SyncVar<string> _username = new();
        private readonly SyncVar<string> _role = new();
        private readonly SyncVar<AvatarDefinition> _avatarDefinition = new();
        
        private void Awake()
        {
            _username.OnChange += OnUsernameChanged;
            _avatarDefinition.OnChange += OnAvatarChanged;
        }

        public override void OnStartClient()
        {
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(IsOwner);

            if (AvatarInstance != null)
                AvatarInstance.IsLocalPlayer = IsOwner;

            if (_IKAssigner != null)
            {
                _IKAssigner.InitializeFollow();
                _IKAssigner.Assign(AvatarInstance);
            }
            RefreshUserName();
            AppCore.Services.Get<NetworkPlayerManager>().RegisterPlayer(this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            AppCore.Services.Get<NetworkPlayerManager>().RegisterPlayer(this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (AppCore.Services.TryGet<NetworkPlayerManager>(out var playerManager))
            { 
                playerManager.UnregisterPlayer(this);
            }
        }
        
        [Server]
        public void ApplyProfile(PlayerProfile profile)
        {
            _username.Value = profile.username;
            _role.Value = profile.role;
            _color.Value = profile.color;
            ApplyAvatar(profile.avatar);
            RefreshUserName();
        }

        [Server]
        private void ApplyAvatar(AvatarDefinition avatarDef)
        {
            _avatarDefinition.Value = avatarDef;
        }

        [ServerRpc]
        private void CmdSetAvatar(AvatarDefinition avatarDef)
        {
            ApplyAvatar(avatarDef);
        }

        [Client]
        public void SetAvatar(AvatarDefinition avatarDef)
        {
            CmdSetAvatar(avatarDef);
        }

        private void RefreshUserName()
        {
            var username = string.IsNullOrEmpty(Username) ? "Unknown" : Username;
            var objName = $"Player '{username}' ({OwnerId})";
            if (IsOwner) objName += " [Local]";
            gameObject.name = objName;
            
            if (_NameTag != null)
            {
                _NameTag.SetIsOwner(IsOwner);
                _NameTag.SetText(Username);
            }
        }
        
        private void OnUsernameChanged(string oldValue, string newValue, bool asServer)
        {
            RefreshUserName();
        }

        private void OnAvatarChanged(AvatarDefinition oldValue, AvatarDefinition newValue, bool asServer)
        {
            AvatarChanged?.Invoke(newValue);
            CreateLocalAvatar(newValue);
        }

        private void CreateLocalAvatar(AvatarDefinition def)
        {
            if (def == null || !def.IsValid())
            {
                Debug.LogError($"Invalid avatar ID {def.UniqueID}");
                return;
            }
            
            if (AvatarInstance != null && AvatarInstance.Definition != def)
            {
                Destroy(AvatarInstance.gameObject);
                AvatarInstance = null;
            }
            if (AvatarInstance == null)
            {
                AvatarInstance = Instantiate(def.AvatarPrefab, _AvatarRoot, false);
                AvatarInstance.Definition = def;
                AvatarInstance.gameObject.name = def.AvatarPrefab.name;
                AvatarInstance.IsLocalPlayer = IsOwner;
                AvatarInstanceChanged?.Invoke(AvatarInstance);
            }

            if (_IKAssigner != null)
                _IKAssigner.Assign(AvatarInstance);
        }

        public Transform GetWorldPoseTransform()
        {
            if (AvatarInstance != null)
                return AvatarInstance.transform;

            if (_AvatarRoot != null && _AvatarRoot.childCount > 0)
                return _AvatarRoot.GetChild(0);

            return _AvatarRoot != null ? _AvatarRoot : transform;
        }

        [Server]
        public void ServerRequestTeleportOwner(Vector3 position, Quaternion rotation)
        {
            TargetRequestTeleport(Owner, position, rotation);
        }

        [TargetRpc]
        private void TargetRequestTeleport(NetworkConnection conn, Vector3 position, Quaternion rotation)
        {

            if (AppCore.Services.TryGet<ControlsManager>(out var cm))
            {
                cm.RequestTeleport(position, rotation);
            }
            else
            {
                Debug.LogWarning("[SUMMON] ControlsManager service not found - cannot teleport.");
            }
        }
    }
}
