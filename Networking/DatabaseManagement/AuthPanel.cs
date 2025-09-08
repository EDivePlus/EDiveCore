// Author: Radim Holub
// Created: 08.09.2025

using EDIVE.XRTools.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Networking.DatabaseManagement
{
    public class AuthPanel : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField]
        private AuthService _auth;
        [SerializeField]
        private TokenStore _tokenStore;
        [SerializeField]
        private TMP_InputField _emailInput;
        [SerializeField]
        private TMP_InputField _passwordInput;
        [SerializeField]
        private Button _loginButton;
        [SerializeField]


        private void Awake()
        {
            _loginButton.onClick.AddListener(OnLoginClicked);
        }

        private void Start()
        {
            _auth.TryLoadStoredToken();

            if (_auth.IsLoggedIn)
            {
                Debug.Log($"Přihlášen jako {_tokenStore.UserId}");
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
        }
        

        private void OnToggleShowPassword(bool show)
        {
            _passwordInput.contentType = show ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
            _passwordInput.ForceLabelUpdate();
        }

        private void OnLoginClicked()
        {
            var email = _emailInput.text?.Trim();
            var pass = _passwordInput.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                Debug.Log("Vyplň e-mail i heslo.");
                return;
            }

            Debug.Log("Přihlašuji…");
            _auth.Login(email, pass);
        }

        private void OnLogoutClicked()
        {
            _tokenStore.Clear();
            SetLoggedInUI(false);
            Debug.Log("Odhlášeno.");
        }

        private void OnLoginOk(LoginResponse r)
        {
            SetLoggedInUI(true);
            
            Debug.Log("Přihlášení proběhlo úspěšně.");
            Debug.Log($"Access Token: {r.AccessToken}");
            
            Debug.Log($"Refresh Token: {r.RefreshToken}");
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
