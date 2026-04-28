// Author: Radim Holub
// Created: 08.09.2025

using System;
using EDIVE.Core;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Auth
{
    public class AuthLoginPanel : MonoBehaviour
    {
        [EnhancedBoxGroup("Credentials login", Color = "@ColorTools.Cyan")]
        [SerializeField]
        [Required]
        private TMP_InputField _CredentialsEmailInput;
        
        [EnhancedBoxGroup("Credentials login")]
        [SerializeField]
        [Required]
        private TMP_InputField _CredentialsPasswordInput;
        
        [EnhancedBoxGroup("Credentials login")]
        [SerializeField]
        [Required]
        private Button _CredentialsLoginButton;
        
        [EnhancedBoxGroup("Credentials login")]
        [SerializeField]
        private Button _CredentialsTogglePasswordButton;
        
        [EnhancedBoxGroup("Anonymous login", Color = "@ColorTools.Green")]
        [SerializeField]
        private Button _AnonymousLoginButton;
        
        [SerializeField]
        [EnhancedBoxGroup("Error display", Color = "@ColorTools.Red")]
        private AToggleState _ErrorState;
        
        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        private TMP_Text _ErrorText;
        
        [SerializeField]
        [Optional]
        private Button _LogoutButton;

        private ServiceHubManager _serviceHubManager;
        private bool _isPasswordHidden = true;

        private void Awake()
        {
            _serviceHubManager = AppCore.Services.Get<ServiceHubManager>();
        }

        private void OnEnable()
        {
            _serviceHubManager.TryLoadStoredToken();

            if (ServiceHubManager.IsLoggedIn)
            {
                Debug.Log($"Logged in (UserId): {AuthStorage.GetUserId()}");
                SetLoggedInUI(true);
            }
            else
            {
                Debug.Log("Enter login credentials.");
                SetLoggedInUI(false);
            }

            var lastEmail = AuthStorage.GetLastEmail(); // see previous step with storing the email
            if (!string.IsNullOrEmpty(lastEmail))
            {
                _CredentialsEmailInput.SetTextWithoutNotify(lastEmail);
                _CredentialsEmailInput.caretPosition = _CredentialsEmailInput.text.Length;
            }

            _isPasswordHidden = true;
            ApplyPasswordMaskState();

            _serviceHubManager.OnLoginSucceeded += OnLoginOk;
            _serviceHubManager.OnLoginFailed += OnLoginFail;

            if (_ErrorText)
            {
                _ErrorText.text = string.Empty;
                _ErrorText.gameObject.SetActive(false);
            }
            if (_ErrorState) _ErrorState.SetState(false);
            
            _CredentialsLoginButton.onClick.AddListener(OnLoginClicked);
            if (_LogoutButton) _LogoutButton.onClick.AddListener(OnLogoutClicked);
            _CredentialsEmailInput.onEndEdit.AddListener(OnEmailEndEdit);
            if (_CredentialsTogglePasswordButton != null) _CredentialsTogglePasswordButton.onClick.AddListener(OnTogglePasswordClicked);
            if (_AnonymousLoginButton != null) _AnonymousLoginButton.onClick.AddListener(OnAnonymousLoginClicked);
        }
        
        private void OnDisable()
        {
            _serviceHubManager.OnLoginSucceeded -= OnLoginOk;
            _serviceHubManager.OnLoginFailed -= OnLoginFail;
            _CredentialsLoginButton.onClick.RemoveListener(OnLoginClicked);
            if (_LogoutButton) _LogoutButton.onClick.RemoveListener(OnLogoutClicked);
            _CredentialsEmailInput.onEndEdit.RemoveListener(OnEmailEndEdit);
            if (_CredentialsTogglePasswordButton != null) _CredentialsTogglePasswordButton.onClick.RemoveListener(OnTogglePasswordClicked);
            if (_AnonymousLoginButton != null) _AnonymousLoginButton.onClick.RemoveListener(OnAnonymousLoginClicked);
        }

        private void OnLoginClicked()
        {
            var email = _CredentialsEmailInput.text?.Trim();
            var pass = _CredentialsPasswordInput.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                Debug.Log("Fill in both email and password.");
                return;
            }

            Debug.Log("Logging in…");
            _serviceHubManager.Login(email, pass);
        }
        
        private void OnAnonymousLoginClicked()
        {
            Debug.Log("Logging in anonymously…");
            _serviceHubManager.AnonymousLogin();
        }

        private void OnLoginOk(LoginResponse r)
        {
            SetLoggedInUI(true);
            Debug.Log("Login was successful.");
            // Debug.Log($"Access Token: {r.AccessToken}");

            var expUnix = JwtUtils.GetUnixExp(r.AccessToken);
            if (expUnix.HasValue)
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(expUnix.Value).UtcDateTime;
                Debug.Log($"JWT exp: {dt:O} (UTC)");
            }

            var sub = JwtUtils.GetClaim(r.AccessToken, "sub");
            if (!string.IsNullOrEmpty(sub))
                Debug.Log($"JWT sub: {sub}");

            var emailNow = _CredentialsEmailInput.text?.Trim();
            if (!string.IsNullOrEmpty(emailNow))
                AuthStorage.SetLastEmail(emailNow);
        }

        private void OnLoginFail(long status, string message)
        {
            Debug.Log($"{message} {(status > 0 ? $"(HTTP {status})" : "")}");
            if (_ErrorText != null)
            {
                _ErrorText.text = message;
                _ErrorText.gameObject.SetActive(true);
                if (_ErrorState) _ErrorState.SetState(true);
            }
        }

        private void SetLoggedInUI(bool logged)
        {
            _CredentialsEmailInput.interactable = !logged;
            _CredentialsPasswordInput.interactable = !logged;
            _CredentialsLoginButton.interactable = !logged;
            if (_LogoutButton) _LogoutButton.interactable = logged;

            if (logged)
            {
                _isPasswordHidden = true;
                _CredentialsPasswordInput.SetTextWithoutNotify("*****");
                ApplyPasswordMaskState();
                if (_CredentialsTogglePasswordButton != null) _CredentialsTogglePasswordButton.interactable = false;
            }
            else
            {
                _isPasswordHidden = true;
                _CredentialsPasswordInput.SetTextWithoutNotify(string.Empty);
                ApplyPasswordMaskState();
                if (_CredentialsTogglePasswordButton != null) _CredentialsTogglePasswordButton.interactable = true;
            }
        }

        private void OnLogoutClicked()
        {
            _serviceHubManager.Logout();
            SetLoggedInUI(false);
            Debug.Log("User has been logged out.");
        }

        private void OnEmailEndEdit(string value)
        {
            var v = value?.Trim();
            if (!string.IsNullOrEmpty(v))
                AuthStorage.SetLastEmail(v);
        }

        private void ApplyPasswordMaskState()
        {
            if (_CredentialsPasswordInput == null) return;
            _CredentialsPasswordInput.contentType = _isPasswordHidden
                ? TMP_InputField.ContentType.Password
                : TMP_InputField.ContentType.Standard;
            _CredentialsPasswordInput.ForceLabelUpdate();
        }

        private void OnTogglePasswordClicked()
        {
            _isPasswordHidden = !_isPasswordHidden;
            ApplyPasswordMaskState();
        }
    }
    
}
