// Author: Michal Petr
// Created: 20.04.2026

using EDIVE.ServiceHub.Auth;
using EDIVE.ServiceHub.RemoteContent;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.UIElements;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.ServiceHub
{
    public class ServiceHubUserDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _UserIdText;
        
        [SerializeField]
        private TMP_Text _DisplayNameText;
        
        [SerializeField]
        private TMP_Text _EmailText;
        
        [SerializeField]
        private TMP_Text _RolesText;
        
        [SerializeField]
        [PropertySpace]
        private ProfilePictureDisplay _ProfilePictureDisplay;
        
        [SerializeField]
        [PropertySpace]
        private AToggleState _IsAnonymousToggle;
        
        [SerializeField]
        [PropertySpace]
        [PropertyTooltip("Automatically gets auth info from storage on enable and updates display.")]
        private bool _AutoGetAuthInfo;

        private void OnEnable()
        {
            if (_AutoGetAuthInfo)
            {
                var userInfo = AuthStorage.Client.GetInfo<AuthUserInfo>();
                if (userInfo != null)
                    SetUserInfo(userInfo);
            }
        }

        public void SetUserInfo(AuthUserInfo userInfo)
        {
            if (_DisplayNameText) _DisplayNameText.text = userInfo.Name;
            if (_EmailText) _EmailText.text = userInfo.Email;
            if (_RolesText) _RolesText.text = string.Join(", ", userInfo.Roles);
            if (_UserIdText) _UserIdText.text = userInfo.Id;
            
            if (_ProfilePictureDisplay) _ProfilePictureDisplay.SetProfilePictureFromName(!string.IsNullOrEmpty(userInfo.Name) ? userInfo.Name : "?");
            
            if (_IsAnonymousToggle) _IsAnonymousToggle.SetState(userInfo.IsAnonymous);
        }

        public void SetUserInfo(ContentItemOwnerInfo owner)
        {
            if (_DisplayNameText) _DisplayNameText.text = owner.DisplayName;
            if (_UserIdText) _UserIdText.text = owner.Id;
            
            if (_ProfilePictureDisplay) _ProfilePictureDisplay.SetProfilePictureFromName(!string.IsNullOrEmpty(owner.DisplayName) ? owner.DisplayName : "?");
        }
    }
}
