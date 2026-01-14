// Author: František Holubec
// Created: 23.04.2025

using EDIVE.AssetTranslation;
using EDIVE.Avatars;
using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

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
        
        [SerializeField] private BillboardNameTag _NameTagPrefab; 
        private BillboardNameTag _nameTagInstance;
        [SerializeField] private Transform _HeadOverride;

        private readonly SyncVar<Color> _color = new(Color.white);
        private readonly SyncVar<string> _username = new();
        private readonly SyncVar<string> _role = new();
        private readonly SyncVar<string> _avatarID = new();

        private AvatarController _avatarInstance;

        public Transform AvatarRoot => _AvatarRoot;

        public string Username => _username.Value;
        public string Role => _role.Value;
        public Color Color => _color.Value;
        public string AvatarID => _avatarID.Value;
        
        private void Awake()
        {
            _username.OnChange += OnUsernameChanged;
            _avatarID.OnChange += OnAvatarChanged;
        }

        public override void OnStartClient()
        {
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(IsOwner);

            if (_avatarInstance != null)
                _avatarInstance.IsLocalPlayer = IsOwner;

            if (_IKAssigner != null)
            {
                _IKAssigner.InitializeFollow();
                _IKAssigner.Assign(_avatarInstance);
            }
            RefreshGameObjectName();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
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
            ApplyAvatar(profile.avatarId);
            RefreshGameObjectName();
        }

        [Server]
        private void ApplyAvatar(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId))
                return;

            if (_avatarID.Value != avatarId)
            {
                _avatarID.Value = avatarId;
                //CreateLocalAvatar(avatarId); // Will be created on OnAvatarChanged
            }
        }

        [ServerRpc]
        private void CmdSetAvatar(string avatarId)
        {
            ApplyAvatar(avatarId);
        }

        [Client]
        public void SetAvatar(AvatarDefinition avatarDefinition)
        {
            CmdSetAvatar(avatarDefinition.UniqueID);
        }

        private void RefreshGameObjectName()
        {
            var username = string.IsNullOrEmpty(Username) ? "Unknown" : Username;
            var objName = $"Player '{username}' ({OwnerId})";
            if (IsOwner) objName += " [Local]";
            gameObject.name = objName;
        }
        
        private void OnUsernameChanged(string oldValue, string newValue, bool asServer)
        {
            RefreshGameObjectName();

            if (_nameTagInstance != null)
                _nameTagInstance.SetText(newValue);
            else
                TrySetupNameTag();
        }

        private void OnAvatarChanged(string oldValue, string newValue, bool asServer)
        {
            CreateLocalAvatar(newValue);
            if (IsOwner)
            {
                var mgr = AppCore.Services.Get<NetworkPlayerManager>();
                mgr?.OnLocalAvatarChanged(newValue);
            }
        }

        private void CreateLocalAvatar(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId))
                return;
            
            if (!DefinitionTranslationUtils.TryGetDefinition<AvatarDefinition>(avatarId, out var def) || !def.IsValid())
            {
                Debug.LogError($"Invalid avatar ID {avatarId}");
                return;
            }
            
            if (_avatarInstance != null && _avatarInstance.Definition != def)
            {
                Destroy(_avatarInstance.gameObject);
                _avatarInstance = null;
            }
            if (_avatarInstance == null)
            {
                _avatarInstance = Instantiate(def.AvatarPrefab, _AvatarRoot, false);
                _avatarInstance.Definition = def;
                _avatarInstance.gameObject.name = def.AvatarPrefab.name;
                _avatarInstance.IsLocalPlayer = IsOwner;
            }

            if (_IKAssigner != null)
                _IKAssigner.Assign(_avatarInstance);
            
            if (_nameTagInstance != null)
            {
                Destroy(_nameTagInstance.gameObject);
                _nameTagInstance = null;
            }
            TrySetupNameTag();
        }
        private void TrySetupNameTag()
        {
            if (_nameTagInstance != null) return;
            if (_avatarInstance == null) return;
            if (string.IsNullOrWhiteSpace(Username)) return;
            
            Transform head = _HeadOverride;

            if (_NameTagPrefab != null)
            {
                _nameTagInstance = Instantiate(_NameTagPrefab, head, false);
                _nameTagInstance.BindHead(head);
                _nameTagInstance.SetIsOwner(IsOwner);
                _nameTagInstance.SetText(Username);
            }
        }
        
    }
}
