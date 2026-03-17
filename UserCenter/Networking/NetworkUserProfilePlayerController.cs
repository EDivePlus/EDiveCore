// Author: Michal Petr
// Created: 17.03.2026

using EDIVE.Networking.UI;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace EDIVE.UserCenter.Networking
{
    public class NetworkUserProfilePlayerController : NetworkBehaviour
    {
        [SerializeField]
        private BillboardNameTag _NameTag;
        
        public string Username => _username.Value;

        private readonly SyncVar<string> _username = new();

        private void Awake()
        {
            _username.OnChange += OnUsernameChanged;
        }

        public override void OnStartClient()
        {
            RefreshUserName();
        }
        
        [Server]
        public void ApplyProfile(UserCenterProfile profile)
        {
            _username.Value = profile.Username;
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
    }
}
