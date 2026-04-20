// Author: Michal Petr
// Created: 08.04.2026

using System;
using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UserCenter.Auth
{
    public class AuthPanelController : MonoBehaviour
    {
        [SerializeField]
        private AToggleState _LoggedInToggle;
        
        [SerializeField]
        private Button _LogoutButton;
        
        private UserCenterManager _userCenter;

        private void Awake()
        {
            _userCenter = AppCore.Services.Get<UserCenterManager>();
        }

        private void OnEnable()
        {
            _userCenter.OnLoginSucceeded += OnLoginSucceeded;
            _userCenter.OnLoginFailed += OnLoginFailed;
            _LogoutButton.onClick.AddListener(LogOut);
            RefreshUI();
        }

        private void OnDisable()
        {
            _userCenter.OnLoginSucceeded -= OnLoginSucceeded;
            _userCenter.OnLoginFailed -= OnLoginFailed;
            _LogoutButton.onClick.RemoveListener(LogOut);
        }
        
        private void OnLoginSucceeded(LoginResponse response)
        {
            RefreshUI();
        }
        
        private void OnLoginFailed(long statusCode, string errorMessage)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (UserCenterManager.IsLoggedIn)
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
            _userCenter.Logout();
            RefreshUI();
        }
    }
}
