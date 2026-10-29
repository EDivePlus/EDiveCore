// Author: Michal Petr
// Created: 08.04.2026

using Cysharp.Threading.Tasks;
using EDIVE.Conditions;
using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.View.ViewTree;
using EDIVE.View.Windows;
using EDIVE.View.Windows.Dialog;
using EDIVE.View.Windows.Screens;
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

        [SerializeField]
        private WindowDefinition _DialogWindow;

        [SerializeField]
        [Tooltip("Result true logs out")]
        private DialogSetup _LogoutDialog;

        [SerializeReference]
        [Tooltip("Asks only while this holds, always when empty")]
        private ICondition _ConfirmCondition;

        private ClientAuthService _clientAuth;
        private bool _confirming;

        private void Awake()
        {
            _clientAuth = AppCore.Services.Get<ServiceHubManager>().ClientAuth;
        }

        private void OnEnable()
        {
            _clientAuth.OnLoginSucceeded += OnClientLoginSucceeded;
            _clientAuth.OnLoginFailed += OnClientLoginFailed;
            _clientAuth.OnLoggedOut += RefreshUI;
            _LogoutButton.onClick.AddListener(LogOut);
            RefreshUI();
        }

        private void OnDisable()
        {
            _clientAuth.OnLoginSucceeded -= OnClientLoginSucceeded;
            _clientAuth.OnLoginFailed -= OnClientLoginFailed;
            _clientAuth.OnLoggedOut -= RefreshUI;
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

        private void LogOut() => LogOutAsync().Forget();

        private async UniTaskVoid LogOutAsync()
        {
            if (_confirming)
                return;
            var screen = _DialogWindow && _LogoutDialog && (_ConfirmCondition == null || _ConfirmCondition.Evaluate()) ? this.FindInViewTree<ScreenRoot>() : null;
            if (screen != null)
            {
                _confirming = true;
                object result;
                try
                {
                    result = await screen.Open(_DialogWindow, _LogoutDialog).Closed;
                }
                finally
                {
                    _confirming = false;
                }
                if (result is not true || !this)
                    return;
            }

            _clientAuth.LogoutClient();
            RefreshUI();
        }
    }
}
