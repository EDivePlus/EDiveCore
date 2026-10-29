// Author: František Holubec
// Created: 28.10.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Auth
{
    // Signs in with a code the user approves on the website from a phone or PC
    public class DeviceCodePanel : MonoBehaviour
    {
        public enum DeviceCodeState
        {
            Hidden,
            Waiting,
            Denied,
            Expired
        }

        [SerializeField]
        [Required]
        private AMultiState _State;

        [SerializeField]
        [Required]
        private Button _StartButton;

        [SerializeField]
        [Required]
        private Button _CancelButton;

        [SerializeField]
        [Required]
        private Button _NewCodeButton;

        [SerializeField]
        [Required]
        private TMP_Text _CodeText;

        [SerializeField]
        [Required]
        private TMP_Text _UrlText;

        [SerializeField]
        [Required]
        private TMP_Text _CountdownText;

        private ClientAuthService _clientAuth;
        private CancellationTokenSource _cancellation;

        private void Awake()
        {
            _clientAuth = AppCore.Services.Get<ServiceHubManager>().ClientAuth;
        }

        private void OnEnable()
        {
            _StartButton.onClick.AddListener(Begin);
            _NewCodeButton.onClick.AddListener(Begin);
            _CancelButton.onClick.AddListener(Cancel);
            Cancel();
        }

        private void OnDisable()
        {
            _StartButton.onClick.RemoveListener(Begin);
            _NewCodeButton.onClick.RemoveListener(Begin);
            _CancelButton.onClick.RemoveListener(Cancel);
            Stop();
        }

        private void SetState(DeviceCodeState state) => _State.SetState(state);

        private void Begin()
        {
            Stop();
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RunAsync(_cancellation.Token).Forget();
        }

        private void Cancel()
        {
            Stop();
            SetState(DeviceCodeState.Hidden);
        }

        private void Stop()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
        }

        private static string ShortUrl(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host + uri.AbsolutePath.TrimEnd('/') : url ?? "";

        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            _StartButton.interactable = _NewCodeButton.interactable = false;
            var response = await _clientAuth.RequestDeviceCodeAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            _StartButton.interactable = _NewCodeButton.interactable = true;

            // The login panel shows why
            if (!response.IsSuccess)
            {
                SetState(DeviceCodeState.Hidden);
                return;
            }

            var code = response.Result;
            _CodeText.text = code.Code;
            _UrlText.text = ShortUrl(code.VerifyUrl);
            var expiresAt = DateTime.UtcNow.AddSeconds(code.ExpiresIn);
            var interval = Mathf.Max(1, code.Interval);
            SetState(DeviceCodeState.Waiting);

            var nextPoll = Time.realtimeSinceStartup + interval;
            while (!cancellationToken.IsCancellationRequested)
            {
                var left = expiresAt - DateTime.UtcNow;
                if (left <= TimeSpan.Zero)
                {
                    SetState(DeviceCodeState.Expired);
                    return;
                }
                _CountdownText.text = $"{(int) left.TotalMinutes}:{left.Seconds:00}";

                if (Time.realtimeSinceStartup >= nextPoll)
                {
                    var poll = await _clientAuth.PollDeviceCodeAsync(code.DeviceSecret, cancellationToken);
                    nextPoll = Time.realtimeSinceStartup + interval;
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    if (poll.IsSuccess)
                    {
                        SetState(DeviceCodeState.Hidden);
                        return;
                    }

                    switch (poll.ApiStatus)
                    {
                        case (int) ServiceHubStatus.DeviceCodePending:
                            break;
                        case (int) ServiceHubStatus.DeviceCodeDenied:
                            SetState(DeviceCodeState.Denied);
                            return;
                        case (int) ServiceHubStatus.DeviceCodeExpired:
                            SetState(DeviceCodeState.Expired);
                            return;
                        default:
                            // Banned or unknown app ends it, the login panel shows the error. Offline keeps polling
                            if (poll.ApiStatus > 0)
                            {
                                SetState(DeviceCodeState.Hidden);
                                return;
                            }
                            break;
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime, cancellationToken: cancellationToken).SuppressCancellationThrow();
            }
        }
    }
}
