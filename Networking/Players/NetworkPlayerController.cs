// Author: František Holubec
// Created: 23.04.2025

using EDIVE.Avatars;
using EDIVE.Core;
using EDIVE.Networking.UI;
using EDIVE.Networking.Utils;
using EDIVE.StateHandling.ToggleStates;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using FishNet.Connection;
using EDIVE.XRTools.Controls;
using FishNet;

namespace EDIVE.Networking.Players
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField]
        private IKTargetAssigner _IKAssigner;

        [SerializeField]
        private BillboardNameTag _NameTag;
        
        [SerializeField]
        private AToggleState _LocalPlayerToggle;
        
        public string Username => _username.Value;
        public string Role => _role.Value;
        public Color Color => _color.Value;
        public AvatarDefinition AvatarDefinition => _avatarDefinition.Value;
        
        private readonly SyncVar<Color> _color = new(Color.white);
        private readonly SyncVar<string> _username = new();
        private readonly SyncVar<string> _role = new();
        private readonly SyncVar<AvatarDefinition> _avatarDefinition = new();
        
        private readonly SyncVar<int> _avatarObjectId = new(0);
        private readonly SyncVarNetworkBehaviourResolver<AvatarController> _avatarResolver = new();
        
        public AvatarController AvatarInstance => _avatarResolver.Value;

        private void Awake()
        {
            _username.OnChange += OnUsernameChanged;

            _avatarResolver.BindToSyncVar(_avatarObjectId);
            _avatarResolver.OnChanged += OnAvatarChanged;
        }
        
        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            if(_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(IsOwner);
        }
        
        private void OnAvatarChanged(AvatarController avatar)
        {
            if (avatar == null) 
                return;
            
            avatar.NetworkObject.SetParent(this);
            
            if (_IKAssigner != null)
                _IKAssigner.Assign(avatar);
        }
        
        public override void OnStartClient()
        {
            RefreshUserName();
            AppCore.Services.Get<NetworkPlayerManager>().RegisterPlayer(this);
        }

        public override void OnStartServer()
        {
            AppCore.Services.Get<NetworkPlayerManager>().RegisterPlayer(this);
        }

        public override void OnStopNetwork()
        {
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
            ServerApplyAvatar(profile.avatar);
        }
        
        [Server]
        private void ServerApplyAvatar(AvatarDefinition avatarDef)
        {
            if (_avatarDefinition.Value != avatarDef)
                _avatarDefinition.Value = avatarDef;
            
            if (avatarDef == null || !avatarDef.IsValid())
            {
                Debug.LogError($"Invalid avatar definition");
                return;
            }
            
            var networkManager = InstanceFinder.NetworkManager;
            
            // Despawn existing avatar if any
            if (_avatarResolver.Value != null)
            {
                networkManager.ServerManager.Despawn(_avatarResolver.Value.gameObject);
                _avatarResolver.SetValue(null);
            }

            // Spawn new avatar
            var netObj = networkManager.GetPooledInstantiated(avatarDef.AvatarPrefab.gameObject, true);
            networkManager.ServerManager.Spawn(netObj, Owner);
            var avatar = netObj.GetComponent<AvatarController>();
            
            _avatarResolver.SetValue(avatar);
        }
        
        [ServerRpc]
        public void SetAvatar(AvatarDefinition avatarDef)
        {
            ServerApplyAvatar(avatarDef);
        }

        private void RefreshUserName()
        {
            var username = string.IsNullOrEmpty(Username) ? "Unknown" : Username;
            var objName = $"Player '{username}' ({OwnerId})";
            if (IsOwner) objName += " [Local]";
            gameObject.name = objName;

            if (_NameTag != null) 
                _NameTag.SetText(Username);
        }
        
        private void OnUsernameChanged(string oldValue, string newValue, bool asServer)
        {
            RefreshUserName();
        }
        
        public Transform GetWorldPoseTransform()
        {
            if (_avatarResolver.Value != null)
                return _avatarResolver.Value.transform;

            return transform;
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
