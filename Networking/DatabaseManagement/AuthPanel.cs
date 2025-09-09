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
        [SerializeField] private AuthService _auth;
        [SerializeField] private TMP_InputField _emailInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private Button _loginButton;

 private void Awake()
        {
            _loginButton.onClick.AddListener(OnLoginClicked);
        }

        private void Start()
        {
            _auth.TryLoadStoredToken();

            if (_auth.IsLoggedIn)
            {
                Debug.Log($"Přihlášen (UserId): {AuthStorage.GetUserId()}");
                SetLoggedInUI(true);
            }
            else
            {
                Debug.Log("Zadej přihlašovací údaje.");
                SetLoggedInUI(false);
            }

            _auth.OnLoginSucceeded += OnLoginOk;
            _auth.OnLoginFailed += OnLoginFail;
        }

        private void OnDestroy()
        {
            _auth.OnLoginSucceeded -= OnLoginOk;
            _auth.OnLoginFailed -= OnLoginFail;
            _loginButton.onClick.RemoveListener(OnLoginClicked);
        }

        private void OnLoginClicked()
        {
            var email = _emailInput.text?.Trim();
            var pass  = _passwordInput.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                Debug.Log("Vyplň e-mail i heslo.");
                return;
            }

            Debug.Log("Přihlašuji…");
            _auth.Login(email, pass);
        }

        private void OnLoginOk(LoginResponse r)
        {
            SetLoggedInUI(true);
            Debug.Log("Přihlášení proběhlo úspěšně.");
            Debug.Log($"Access Token: {r.AccessToken}");

            var expUnix = JwtUtils.GetUnixExp(r.AccessToken);
            if (expUnix.HasValue)
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(expUnix.Value).UtcDateTime;
                Debug.Log($"JWT exp: {dt:O} (UTC)");
            }

            var sub = JwtUtils.GetClaim(r.AccessToken, "sub");
            if (!string.IsNullOrEmpty(sub))
                Debug.Log($"JWT sub: {sub}");
        }

        private void OnLoginFail(long status, string message)
        {
            Debug.Log($"{message} {(status > 0 ? $"(HTTP {status})" : "")}");
        }

        private void SetLoggedInUI(bool logged)
        {
            _emailInput.interactable = !logged;
            _passwordInput.interactable = !logged;
            _loginButton.interactable = !logged;
        }
    }
}



