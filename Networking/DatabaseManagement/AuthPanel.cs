// Author: Radim Holub
// Created: 08.09.2025

using EDIVE.XRTools.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace EDIVE.Networking.DatabaseManagement
{
    public class AuthPanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private AuthService _Auth;
        [SerializeField] private TMP_InputField _EmailInput;
        [SerializeField] private TMP_InputField _PasswordInput;
        [SerializeField] private Button _LoginButton;
        [SerializeField] private Button _LogoutButton;
        [SerializeField] private Button _TogglePasswordButton; 
        private bool _IsPasswordHidden = true;

 private void Awake()
        {
            _LoginButton.onClick.AddListener(OnLoginClicked);
            _LogoutButton.onClick.AddListener(OnLogoutClicked);
            _EmailInput.onEndEdit.AddListener(OnEmailEndEdit);
            if (_TogglePasswordButton != null) _TogglePasswordButton.onClick.AddListener(OnTogglePasswordClicked);
        }

        private void Start()
        {
            _Auth.TryLoadStoredToken();

            if (_Auth.IsLoggedIn)
            {
                Debug.Log($"Přihlášen (UserId): {AuthStorage.GetUserId()}");
                SetLoggedInUI(true);
            }
            else
            {
                Debug.Log("Zadej přihlašovací údaje.");
                SetLoggedInUI(false);
            }
            
            var lastEmail = AuthStorage.GetLastEmail(); // viz předešlý krok s uložením e-mailu
            if (!string.IsNullOrEmpty(lastEmail))
            {
                _EmailInput.SetTextWithoutNotify(lastEmail);
                _EmailInput.caretPosition = _EmailInput.text.Length;
            }
            
            _IsPasswordHidden = true;
            ApplyPasswordMaskState();

            _Auth.OnLoginSucceeded += OnLoginOk;
            _Auth.OnLoginFailed += OnLoginFail;
        }

        private void OnDestroy()
        {
            _Auth.OnLoginSucceeded -= OnLoginOk;
            _Auth.OnLoginFailed -= OnLoginFail;
            _LoginButton.onClick.RemoveListener(OnLoginClicked);
            _LogoutButton.onClick.RemoveListener(OnLogoutClicked);
            _EmailInput.onEndEdit.RemoveListener(OnEmailEndEdit);
            if (_TogglePasswordButton != null) _TogglePasswordButton.onClick.RemoveListener(OnTogglePasswordClicked);
        }

        private void OnLoginClicked()
        {
            var email = _EmailInput.text?.Trim();
            var pass  = _PasswordInput.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                Debug.Log("Vyplň e-mail i heslo.");
                return;
            }

            Debug.Log("Přihlašuji…");
            _Auth.Login(email, pass);
        }

        private void OnLoginOk(LoginResponse r)
        {
            SetLoggedInUI(true);
            Debug.Log("Přihlášení proběhlo úspěšně.");
            Debug.Log($"Access Token: {r._AccessToken}");

            var expUnix = JwtUtils.GetUnixExp(r._AccessToken);
            if (expUnix.HasValue)
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(expUnix.Value).UtcDateTime;
                Debug.Log($"JWT exp: {dt:O} (UTC)");
            }

            var sub = JwtUtils.GetClaim(r._AccessToken, "sub");
            if (!string.IsNullOrEmpty(sub))
                Debug.Log($"JWT sub: {sub}");
            
            var emailNow = _EmailInput.text?.Trim();
            if (!string.IsNullOrEmpty(emailNow))
                AuthStorage.SetLastEmail(emailNow);
        }

        private void OnLoginFail(long status, string message)
        {
            Debug.Log($"{message} {(status > 0 ? $"(HTTP {status})" : "")}");
        }

        private void SetLoggedInUI(bool logged)
        {
            _EmailInput.interactable = !logged;
            _PasswordInput.interactable = !logged;
            _LoginButton.interactable = !logged;
            _LogoutButton.interactable = logged; 
            
            if (logged)
            {
                _IsPasswordHidden = true;
                _PasswordInput.SetTextWithoutNotify("*****");
                ApplyPasswordMaskState();
                if (_TogglePasswordButton != null) _TogglePasswordButton.interactable = false;
            }
            else
            {
                _IsPasswordHidden = true;
                _PasswordInput.SetTextWithoutNotify(string.Empty);
                ApplyPasswordMaskState();
                if (_TogglePasswordButton != null) _TogglePasswordButton.interactable = true;
            }
        }
        private void OnLogoutClicked()
        {
            _Auth.Logout();
            SetLoggedInUI(false);
            Debug.Log("Uživatel byl odhlášen.");
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
            _PasswordInput.contentType = _IsPasswordHidden
                ? TMP_InputField.ContentType.Password
                : TMP_InputField.ContentType.Standard;
            _PasswordInput.ForceLabelUpdate();
        }
        private void OnTogglePasswordClicked()
        {
            _IsPasswordHidden = !_IsPasswordHidden;
            ApplyPasswordMaskState();
        }
    }
}



