// Author: Michal Petr
// Created: 08.04.2026

using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Auth
{
    public class AuthPanelController : MonoBehaviour
    {
        [SerializeField]
        private AToggleState _LoggedInToggle;

        [SerializeField]
        private Button _LogoutButton;

        private ClientAuthService _clientAuth;

        private void Awake()
        {
            _clientAuth = AppCore.Services.Get<ServiceHubManager>().ClientAuth;
        }

        private void OnEnable()
        {
            _clientAuth.OnLoginSucceeded += OnClientLoginSucceeded;
            _clientAuth.OnLoginFailed += OnClientLoginFailed;
            _LogoutButton.onClick.AddListener(LogOut);
            RefreshUI();
        }

        private void OnDisable()
        {
            _clientAuth.OnLoginSucceeded -= OnClientLoginSucceeded;
            _clientAuth.OnLoginFailed -= OnClientLoginFailed;
            _LogoutButton.onClick.RemoveListener(LogOut);
        }

        private void OnClientLoginSucceeded(LoginResponse response)
        {
            RefreshUI();
        }

        private void OnClientLoginFailed(long statusCode, string errorMessage)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_clientAuth.IsLoggedIn)
            {
                _LoggedInToggle.SetState(true);
                _LogoutButton.interactable = true;
            }
            else
            {
                _LoggedInToggle.SetState(false);
                _LogoutButton.interactable = false;
            }
        }

        private void LogOut()
        {
            _clientAuth.LogoutClient();
            RefreshUI();
        }
    }
}
