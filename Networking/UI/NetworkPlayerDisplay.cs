// Author: Michal Petr
// Created: 21.05.2026

using EDIVE.Networking.Players;
using EDIVE.UIElements;
using TMPro;
using UnityEngine;

namespace EDIVE.Networking.UI
{
    public class NetworkPlayerDisplay : MonoBehaviour
    {
        [SerializeField]
        private ProfilePictureDisplay _ProfilePicture;
        
        [SerializeField]
        private TMP_Text _UserIdText;
        
        [SerializeField]
        private TMP_Text _DisplayNameText;
        
        [SerializeField]
        private TMP_Text _EmailText;
        
        [SerializeField]
        private TMP_Text _PingText;

        public NetworkPlayerController PlayerController { get; private set; }

        public void SetPlayerController(NetworkPlayerController playerController)
        {
            TryUnregisterListeners();
            PlayerController = playerController;

            TryRegisterListeners();
            UpdateDisplay();
        }

        private void OnEnable()
        {
            TryRegisterListeners();
            UpdateDisplay();
        }

        private void OnDisable()
        {
            TryUnregisterListeners();
        }
        
        private void TryRegisterListeners()
        {
            if (PlayerController != null)
            {
                PlayerController.AuthUserInfoChanged += OnAuthUserInfoChanged;
                PlayerController.PingChanged += OnPingChanged;
            }
        }
        
        private void TryUnregisterListeners()
        {
            if (PlayerController != null)
            {
                PlayerController.AuthUserInfoChanged -= OnAuthUserInfoChanged;
                PlayerController.PingChanged -= OnPingChanged;
            }
        }

        private void OnAuthUserInfoChanged(NetworkUserInfo userInfo)
        {
            UpdateDisplay();
        }

        private void OnPingChanged(int ping)
        {
            if (_PingText != null)
                _PingText.text = $"{ping}ms";
        }

        private void UpdateDisplay()
        {
            if (PlayerController == null)
                return;

            var userInfo = PlayerController.AuthUserInfo;
            if (userInfo != null)
            {
                if (_UserIdText != null)
                    _UserIdText.text = userInfo.Id;
                if (_DisplayNameText != null)
                    _DisplayNameText.text = userInfo.Name;
                if (_EmailText != null)
                    _EmailText.text = userInfo.Email;
                if (_ProfilePicture != null)
                    _ProfilePicture.SetProfilePictureFromName(!string.IsNullOrEmpty(userInfo.Name) ? userInfo.Name : "?");
            }
            
            if (_PingText != null)
                _PingText.text = $"{PlayerController.Ping}ms";
        }
    }
}
