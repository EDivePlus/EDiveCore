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
        
        private ServiceHubManager _serviceHub;

        private void Awake()
        {
            _serviceHub = AppCore.Services.Get<ServiceHubManager>();
        }

        private void OnEnable()
        {
            _serviceHub.OnClientLoginSucceeded += OnClientLoginSucceeded;
            _serviceHub.OnClientLoginFailed += OnClientLoginFailed;
            _LogoutButton.onClick.AddListener(LogOut);
            RefreshUI();
        }

        private void OnDisable()
        {
            _serviceHub.OnClientLoginSucceeded -= OnClientLoginSucceeded;
            _serviceHub.OnClientLoginFailed -= OnClientLoginFailed;
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
            if (ServiceHubManager.IsLoggedIn)
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
            _serviceHub.LogoutClient();
            RefreshUI();
        }
    }
}
