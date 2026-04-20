// Author: Radim Holub
// Created: 08.09.2025

using System;
using EDIVE.Core;
using EDIVE.Http;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UserCenter.Auth
{
    public class AuthLoginPanel : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private TMP_InputField _EmailInput;
        
        [SerializeField]
        [Required]
        private TMP_InputField _PasswordInput;
        
        [SerializeField]
        [Required]
        private Button _LoginButton;
        
        [SerializeField]
        [Optional]
        private Button _LogoutButton;
        
        [SerializeField]
        private Button _TogglePasswordButton;
        
        [SerializeField]
        private TMP_Text _ErrorText;

        private UserCenterManager _userCenterManager;
        private bool _isPasswordHidden = true;

        private void Awake()
        {
            _userCenterManager = AppCore.Services.Get<UserCenterManager>();
            _LoginButton.onClick.AddListener(OnLoginClicked);
            if (_LogoutButton) _LogoutButton.onClick.AddListener(OnLogoutClicked);
            _EmailInput.onEndEdit.AddListener(OnEmailEndEdit);
            if (_TogglePasswordButton != null) _TogglePasswordButton.onClick.AddListener(OnTogglePasswordClicked);
        }

        private void OnEnable()
        {
            _userCenterManager.TryLoadStoredToken();

            if (UserCenterManager.IsLoggedIn)
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
                _EmailInput.SetTextWithoutNotify(lastEmail);
                _EmailInput.caretPosition = _EmailInput.text.Length;
            }

            _isPasswordHidden = true;
            ApplyPasswordMaskState();

            _userCenterManager.OnLoginSucceeded += OnLoginOk;
            _userCenterManager.OnLoginFailed += OnLoginFail;

            if (_ErrorText)
            {
                _ErrorText.text = string.Empty;
                _ErrorText.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            _userCenterManager.OnLoginSucceeded -= OnLoginOk;
            _userCenterManager.OnLoginFailed -= OnLoginFail;
            _LoginButton.onClick.RemoveListener(OnLoginClicked);
            if (_LogoutButton) _LogoutButton.onClick.RemoveListener(OnLogoutClicked);
            _EmailInput.onEndEdit.RemoveListener(OnEmailEndEdit);
            if (_TogglePasswordButton != null) _TogglePasswordButton.onClick.RemoveListener(OnTogglePasswordClicked);
        }

        private void OnLoginClicked()
        {
            var email = _EmailInput.text?.Trim();
            var pass = _PasswordInput.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                Debug.Log("Fill in both email and password.");
                return;
            }

            Debug.Log("Logging in…");
            _userCenterManager.Login(email, pass);
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

            var emailNow = _EmailInput.text?.Trim();
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
            }
        }

        private void SetLoggedInUI(bool logged)
        {
            _EmailInput.interactable = !logged;
            _PasswordInput.interactable = !logged;
            _LoginButton.interactable = !logged;
            if (_LogoutButton) _LogoutButton.interactable = logged;

            if (logged)
            {
                _isPasswordHidden = true;
                _PasswordInput.SetTextWithoutNotify("*****");
                ApplyPasswordMaskState();
                if (_TogglePasswordButton != null) _TogglePasswordButton.interactable = false;
            }
            else
            {
                _isPasswordHidden = true;
                _PasswordInput.SetTextWithoutNotify(string.Empty);
                ApplyPasswordMaskState();
                if (_TogglePasswordButton != null) _TogglePasswordButton.interactable = true;
            }
        }

        private void OnLogoutClicked()
        {
            _userCenterManager.Logout();
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
            if (_PasswordInput == null) return;
            _PasswordInput.contentType = _isPasswordHidden
                ? TMP_InputField.ContentType.Password
                : TMP_InputField.ContentType.Standard;
            _PasswordInput.ForceLabelUpdate();
        }

        private void OnTogglePasswordClicked()
        {
            _isPasswordHidden = !_isPasswordHidden;
            ApplyPasswordMaskState();
        }
    }
    
}
