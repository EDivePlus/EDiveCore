// Author: Radim Holub
// Created: 08.09.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Http;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
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

        [EnhancedBoxGroup("Provider login", Color = "@ColorTools.Yellow")]
        [SerializeField]
        [Tooltip("Shown when the provider is set up in ServiceHubSettings")]
        private List<LoginProviderButton> _ProviderButtons = new();

        [SerializeField]
        [EnhancedBoxGroup("Error display", Color = "@ColorTools.Red")]
        private AToggleState _ErrorState;
        
        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        private TMP_Text _ErrorText;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        private LocalizeStringEvent _ErrorLocalize;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        private LocalizedString _InvalidCredentialsMessage;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        private LocalizedString _BannedMessage;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        private LocalizedString _TooManyAttemptsMessage;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        private LocalizedString _UnreachableMessage;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        [Tooltip("Anonymous account of this device was linked meanwhile")]
        private LocalizedString _AccountLinkedMessage;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        [Tooltip("Password account with an unconfirmed email, the server sent a new link")]
        private LocalizedString _EmailNotConfirmedMessage;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        [Tooltip("Any other failure, e.g. a device code that couldn't be requested")]
        private LocalizedString _FailedMessage;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        [Tooltip("On after the server signed this device out")]
        private AToggleState _SignedOutState;

        [SerializeField]
        [EnhancedBoxGroup("Error display")]
        [Tooltip("Login stays locked this long after too many attempts")]
        private float _RateLimitCooldown = 60;
        
        [SerializeField]
        [Optional]
        private Button _LogoutButton;

        private ServiceHubSettings _settings;
        private ClientAuthService _clientAuth;
        private bool _isPasswordHidden = true;
        private bool _coolingDown;

        private void Awake()
        {
            var serviceHub = AppCore.Services.Get<ServiceHubManager>();
            _settings = serviceHub.Settings;
            _clientAuth = serviceHub.ClientAuth;
        }

        private void OnEnable()
        {
            _clientAuth.TryLoadStoredClientToken();

            if (_clientAuth.IsLoggedIn)
            {
                Debug.Log($"Logged in (UserId): {AuthStorage.Client.GetInfo<AuthUserInfo>()?.Id ?? ""}");
                SetLoggedInUI(true);
            }
            else
            {
                Debug.Log("Enter login credentials.");
                SetLoggedInUI(false);
            }

            var lastEmail = AuthStorage.Client.GetLastEmail(); // see previous step with storing the email
            if (!string.IsNullOrEmpty(lastEmail))
            {
                _CredentialsEmailInput.SetTextWithoutNotify(lastEmail);
                _CredentialsEmailInput.caretPosition = _CredentialsEmailInput.text.Length;
            }

            _isPasswordHidden = true;
            ApplyPasswordMaskState();

            _clientAuth.OnLoginSucceeded += OnClientLoginOk;
            _clientAuth.OnLoginFailed += OnClientLoginFail;
            _clientAuth.SessionEnded += OnSessionEnded;

            HideError();
            ShowSessionEnd();
            
            _CredentialsLoginButton.onClick.AddListener(OnLoginClicked);
            if (_LogoutButton) _LogoutButton.onClick.AddListener(OnLogoutClicked);
            _CredentialsEmailInput.onEndEdit.AddListener(OnEmailEndEdit);
            if (_CredentialsTogglePasswordButton != null) _CredentialsTogglePasswordButton.onClick.AddListener(OnTogglePasswordClicked);
            if (_AnonymousLoginButton != null) _AnonymousLoginButton.onClick.AddListener(OnAnonymousLoginClicked);
            foreach (var providerButton in _ProviderButtons)
            {
                providerButton.gameObject.SetActive(_settings.TryGetLoginProvider(providerButton.ProviderId, out _));
                providerButton.Clicked += OnProviderClicked;
            }
        }
        
        private void OnDisable()
        {
            _clientAuth.OnLoginSucceeded -= OnClientLoginOk;
            _clientAuth.OnLoginFailed -= OnClientLoginFail;
            _clientAuth.SessionEnded -= OnSessionEnded;
            _CredentialsLoginButton.onClick.RemoveListener(OnLoginClicked);
            if (_LogoutButton) _LogoutButton.onClick.RemoveListener(OnLogoutClicked);
            _CredentialsEmailInput.onEndEdit.RemoveListener(OnEmailEndEdit);
            if (_CredentialsTogglePasswordButton != null) _CredentialsTogglePasswordButton.onClick.RemoveListener(OnTogglePasswordClicked);
            if (_AnonymousLoginButton != null) _AnonymousLoginButton.onClick.RemoveListener(OnAnonymousLoginClicked);
            foreach (var providerButton in _ProviderButtons)
                providerButton.Clicked -= OnProviderClicked;
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
            _clientAuth.LoginClient(email, pass);
        }
        
        private void OnAnonymousLoginClicked()
        {
            Debug.Log("Logging in anonymously…");
            _clientAuth.AnonymousLoginClient();
        }

        private static void OnProviderClicked(string provider)
        {
            Debug.LogWarning($"[ServiceHub] {provider} login is not implemented yet.");
        }

        private void OnClientLoginOk(LoginResponse r)
        {
            HideError();
            if (_SignedOutState) _SignedOutState.SetState(false);
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
                AuthStorage.Client.SetLastEmail(emailNow);
        }

        // Status is the API status, or the HTTP status when the server sent no API response
        private void OnClientLoginFail(long status, string message)
        {
            Debug.Log($"{message} {(status > 0 ? $"(status {status})" : "")}");
            switch (status)
            {
                case (int) ServiceHubStatus.BannedFromApp:
                    ShowError(_BannedMessage, message);
                    break;
                case (int) ServiceHubStatus.AccountAlreadyLinked:
                    ShowError(_AccountLinkedMessage, message);
                    break;
                case (int) ServiceHubStatus.EmailNotConfirmed:
                    ShowError(_EmailNotConfirmedMessage, message);
                    break;
                case 429:
                    ShowError(_TooManyAttemptsMessage, message);
                    CooldownAsync().Forget();
                    break;
                case (int) ServiceHubStatus.InvalidCredentials:
                case 401:
                    ShowError(_InvalidCredentialsMessage, message);
                    break;
                case var code when ServiceHubErrors.IsUnreachable(code):
                    ShowError(_UnreachableMessage, message);
                    break;
                default:
                    ShowError(_FailedMessage is { IsEmpty: false } ? _FailedMessage : _InvalidCredentialsMessage, message);
                    break;
            }
        }

        private void OnSessionEnded(ServiceHubStatus reason) => ShowSessionEnd();

        private void ShowSessionEnd()
        {
            var reason = _clientAuth.SessionEndReason;
            if (_SignedOutState) _SignedOutState.SetState(reason == ServiceHubStatus.InvalidRefreshToken);
            if (reason == ServiceHubStatus.BannedFromApp)
                ShowError(_BannedMessage, "Banned from app");
        }

        private void ShowError(LocalizedString localized, string fallback)
        {
            if (_ErrorLocalize && localized != null && !localized.IsEmpty)
                _ErrorLocalize.StringReference = localized;
            else if (_ErrorText)
                _ErrorText.text = fallback;
            if (_ErrorText) _ErrorText.gameObject.SetActive(true);
            if (_ErrorState) _ErrorState.SetState(true);
        }

        private void HideError()
        {
            if (_ErrorText) _ErrorText.gameObject.SetActive(false);
            if (_ErrorState) _ErrorState.SetState(false);
        }

        private async UniTaskVoid CooldownAsync()
        {
            if (_coolingDown)
                return;
            _coolingDown = true;
            SetButtonsInteractable(false);
            await UniTask.Delay(TimeSpan.FromSeconds(_RateLimitCooldown), DelayType.Realtime, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
            _coolingDown = false;
            if (this && !_clientAuth.IsLoggedIn)
                SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _CredentialsLoginButton.interactable = interactable;
            if (_AnonymousLoginButton != null) _AnonymousLoginButton.interactable = interactable;
        }

        private void SetLoggedInUI(bool logged)
        {
            _CredentialsEmailInput.interactable = !logged;
            _CredentialsPasswordInput.interactable = !logged;
            _CredentialsLoginButton.interactable = !logged && !_coolingDown;
            if (_AnonymousLoginButton != null) _AnonymousLoginButton.interactable = !logged && !_coolingDown;
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
            _clientAuth.LogoutClient();
            SetLoggedInUI(false);
            Debug.Log("User has been logged out.");
        }

        private void OnEmailEndEdit(string value)
        {
            var v = value?.Trim();
            if (!string.IsNullOrEmpty(v))
                AuthStorage.Client.SetLastEmail(v);
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
