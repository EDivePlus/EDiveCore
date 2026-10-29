// Author: František Holubec
// Created: 17.10.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Localization;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.SerializableDictionary;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Auth
{
    // Links an anonymous account on the website: shows a code, polls until the person finishes there
    public class AccountLinkPanel : MonoBehaviour
    {
        public enum LinkState
        {
            Hidden,
            Offer,
            Waiting,
            Expired,
            Linked
        }

        [SerializeField]
        [Required]
        private AMultiState _State;

        [SerializeField]
        [Required]
        private Button _LinkButton;

        [SerializeField]
        [Required]
        private Button _CancelButton;

        [SerializeField]
        [Required]
        private Button _NewCodeButton;

        [SerializeField]
        [Required]
        private Button _DoneButton;

        [SerializeField]
        [Required]
        private TMP_Text _CodeText;

        [SerializeField]
        [Required]
        private TMP_Text _UrlText;

        [SerializeField]
        [Required]
        private TMP_Text _CountdownText;

        [SerializeField]
        [Required]
        private TMP_Text _EmailText;

        [SerializeField]
        [MinValue(1)]
        private float _PollInterval = 3;

        [SerializeField]
        [EnhancedBoxGroup("Errors")]
        [Tooltip("Shown under the link button when getting a code fails")]
        private LocalizeStringEvent _ErrorLocalize;

        [SerializeField]
        [EnhancedBoxGroup("Errors")]
        [Tooltip("On while an error shows")]
        private AToggleState _ErrorState;

        [SerializeField]
        [EnhancedBoxGroup("Errors")]
        [Tooltip("Failed is the fallback")]
        private SerializableDictionary<ServiceHubError, SafeLocalizedString> _ErrorMessages = new();

        private ClientAuthService _clientAuth;
        private CancellationTokenSource _linkCancellation;
        private DateTime _expiresAt;

        private void Awake()
        {
            _clientAuth = AppCore.Services.Get<ServiceHubManager>().ClientAuth;
        }

        private void OnEnable()
        {
            _LinkButton.onClick.AddListener(StartLink);
            _NewCodeButton.onClick.AddListener(StartLink);
            _CancelButton.onClick.AddListener(CancelLink);
            _DoneButton.onClick.AddListener(CancelLink);
            _clientAuth.UserInfoChanged += OnUserInfoChanged;
            CancelLink();
        }

        private void OnDisable()
        {
            _LinkButton.onClick.RemoveListener(StartLink);
            _NewCodeButton.onClick.RemoveListener(StartLink);
            _CancelButton.onClick.RemoveListener(CancelLink);
            _DoneButton.onClick.RemoveListener(CancelLink);
            _clientAuth.UserInfoChanged -= OnUserInfoChanged;
            StopLink();
        }

        private void OnUserInfoChanged(AuthUserInfo userInfo)
        {
            if (_linkCancellation == null)
                SetState(OfferState(userInfo));
        }

        private static LinkState OfferState(AuthUserInfo userInfo) => userInfo is {IsAnonymous: true} ? LinkState.Offer : LinkState.Hidden;

        private void SetState(LinkState state) => _State.SetState(state);

        private void ShowError(long statusCode, int apiStatus)
        {
            var message = _ErrorMessages.GetMessage(ServiceHubErrors.Classify(statusCode, apiStatus)) ?? _ErrorMessages.GetMessage(ServiceHubError.Failed);
            var shown = message is { IsEmpty: false };
            if (shown && _ErrorLocalize)
                _ErrorLocalize.StringReference = message;
            if (_ErrorState)
                _ErrorState.SetState(shown);
        }

        private void HideError()
        {
            if (_ErrorState) _ErrorState.SetState(false);
        }

        private void StartLink()
        {
            HideError();
            StopLink();
            _linkCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            LinkAsync(_linkCancellation.Token).Forget();
        }

        private void CancelLink()
        {
            HideError();
            StopLink();
            _clientAuth.TryGetAuthUserInfo(out var userInfo);
            SetState(OfferState(userInfo));
        }

        private void StopLink()
        {
            _linkCancellation?.Cancel();
            _linkCancellation?.Dispose();
            _linkCancellation = null;
        }

        private static string ShortUrl(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host + uri.AbsolutePath.TrimEnd('/') : url ?? "";

        private async UniTaskVoid LinkAsync(CancellationToken cancellationToken)
        {
            _LinkButton.interactable = _NewCodeButton.interactable = false;
            var response = await _clientAuth.RequestLinkCodeAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            _LinkButton.interactable = _NewCodeButton.interactable = true;

            if (!response.IsSuccess)
            {
                // Already has an email, the stored info was stale
                if (response.ApiStatus == (int) ServiceHubStatus.AccountAlreadyLinked)
                    await _clientAuth.CheckClientAuthAsync(cancellationToken);
                else
                    ShowError(response.StatusCode, response.ApiStatus);
                return;
            }

            var code = response.Result.Code ?? "";
            _CodeText.text = code.Length == 6 ? $"{code[..3]} {code[3..]}" : code;
            _UrlText.text = ShortUrl(response.Result.LinkUrl);
            _expiresAt = DateTime.UtcNow.AddSeconds(response.Result.ExpiresIn);
            SetState(LinkState.Waiting);

            var nextPoll = Time.realtimeSinceStartup + _PollInterval;
            while (!cancellationToken.IsCancellationRequested)
            {
                var left = _expiresAt - DateTime.UtcNow;
                if (left <= TimeSpan.Zero)
                {
                    SetState(LinkState.Expired);
                    return;
                }
                _CountdownText.text = $"{(int) left.TotalMinutes}:{left.Seconds:00}";

                if (Time.realtimeSinceStartup >= nextPoll)
                {
                    var status = await _clientAuth.GetLinkCodeStatusAsync(cancellationToken);
                    nextPoll = Time.realtimeSinceStartup + _PollInterval;
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    if (status.IsSuccess)
                    {
                        switch (status.Result.State)
                        {
                            case LinkCodeState.Pending:
                                _expiresAt = DateTime.UtcNow.AddSeconds(status.Result.ExpiresIn);
                                break;
                            case LinkCodeState.Linked:
                                _EmailText.text = status.Result.Email;
                                SetState(LinkState.Linked);
                                return;
                            default:
                                SetState(LinkState.Expired);
                                return;
                        }
                    }
                    // Session ended meanwhile, polling can't succeed
                    else if (ServiceHubErrors.IsSignedOut(status.StatusCode, status.ApiStatus))
                    {
                        _clientAuth.TryGetAuthUserInfo(out var userInfo);
                        SetState(OfferState(userInfo));
                        ShowError(status.StatusCode, status.ApiStatus);
                        return;
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime, cancellationToken: cancellationToken).SuppressCancellationThrow();
            }
        }
    }
}
