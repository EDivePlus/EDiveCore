// Author: František Holubec
// Created: 21.10.2026

using System;
using EDIVE.Localization;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Streaming.Sources;
using EDIVE.UIElements;
using EDIVE.UIElements.SDFShapes;
using EDIVE.View.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Streaming.UI
{
    // Someone wants to watch my view: Allow or Deny before ServiceHub's countdown ends
    public class StreamRequestWindow : AWindow<StreamShareRequest, bool>
    {
        [SerializeField]
        [EnhancedBoxGroup("Request")]
        private ProfilePictureDisplay _Avatar;

        [SerializeField]
        [EnhancedBoxGroup("Request")]
        private TMP_Text _NameText;

        [SerializeField]
        [EnhancedBoxGroup("Request")]
        private TMP_Text _InfoText;

        [SerializeField]
        [EnhancedBoxGroup("Request")]
        private SafeLocalizedString _AppInfo = new();

        [SerializeField]
        [EnhancedBoxGroup("Request")]
        private SafeLocalizedString _WebInfo = new();

        [SerializeField]
        [EnhancedBoxGroup("Countdown")]
        private SDFArc _CountdownArc;

        [Tooltip("Smart string, {0} is the seconds left")]
        [SerializeField]
        [EnhancedBoxGroup("Countdown")]
        private TMP_Text _DenyLabel;

        [SerializeField]
        [EnhancedBoxGroup("Countdown")]
        private SafeLocalizedString _DenyText = new();

        [SerializeField]
        [EnhancedBoxGroup("Include")]
        private Toggle _MenusToggle;

        [Tooltip("Shown for web viewers only")]
        [SerializeField]
        [EnhancedBoxGroup("Include")]
        private GameObject _WebOptions;

        [SerializeField]
        [EnhancedBoxGroup("Include")]
        private Toggle _HearingToggle;

        [SerializeField]
        [EnhancedBoxGroup("Include")]
        private Toggle _MicToggle;

        [SerializeField]
        [EnhancedBoxGroup("Buttons")]
        private Button _AllowButton;

        [SerializeField]
        [EnhancedBoxGroup("Buttons")]
        private Button _DenyButton;

        private StreamShareRequest _request;
        private double _duration;
        private int _shownSeconds = -1;

        protected override void Awake()
        {
            base.Awake();
            if (_AllowButton) _AllowButton.onClick.AddListener(OnAllowClicked);
            if (_DenyButton) _DenyButton.onClick.AddListener(OnDenyClicked);
        }

        protected override void OnDestroy()
        {
            if (_AllowButton) _AllowButton.onClick.RemoveListener(OnAllowClicked);
            if (_DenyButton) _DenyButton.onClick.RemoveListener(OnDenyClicked);
            Unbind();
            base.OnDestroy();
        }

        protected override void OnContextSet(StreamShareRequest request)
        {
            Unbind();
            _request = request;
            if (_request == null)
            {
                Debug.LogError($"[StreamRequestWindow] {name} opened without a request.", this);
                return;
            }

            _request.Closed += OnRequestClosed;
            _duration = Math.Max(1, _request.Remaining.TotalSeconds);
            _shownSeconds = -1;

            if (_Avatar) _Avatar.SetProfilePictureFromName(_request.FromName);
            if (_NameText) _NameText.text = _request.FromName;
            if (_InfoText) _InfoText.text = _request.FromWeb ? _WebInfo : _AppInfo;

            if (_MenusToggle) _MenusToggle.SetIsOnWithoutNotify(true);
            if (_HearingToggle) _HearingToggle.SetIsOnWithoutNotify(false);
            if (_MicToggle) _MicToggle.SetIsOnWithoutNotify(false);
            if (_WebOptions) _WebOptions.SetActive(_request.FromWeb);
            RefreshCountdown();
        }

        protected override void OnWindowClosed()
        {
            // Closed without an answer counts as Deny
            _request?.Deny();
            Unbind();
        }

        private void Update()
        {
            if (_request != null)
                RefreshCountdown();
        }

        private void RefreshCountdown()
        {
            var remaining = Math.Max(0, _request.Remaining.TotalSeconds);
            if (_CountdownArc) _CountdownArc.Value = (float) (remaining / _duration);

            var seconds = (int) Math.Ceiling(remaining);
            if (seconds == _shownSeconds || !_DenyLabel)
                return;
            _shownSeconds = seconds;
            _DenyLabel.text = _DenyText.GetLocalizedString(seconds);
        }

        private void OnAllowClicked()
        {
            _request?.Allow(IsOn(_MenusToggle), IsOn(_HearingToggle), IsOn(_MicToggle));
            Close(true);
        }

        private void OnDenyClicked()
        {
            _request?.Deny();
            Close(false);
        }

        private void OnRequestClosed(StreamShareRequest _)
        {
            Unbind();
            if (State is not WindowState.Closing and not WindowState.Closed)
                Close(false);
        }

        private void Unbind()
        {
            if (_request != null)
                _request.Closed -= OnRequestClosed;
            _request = null;
        }

        private static bool IsOn(Toggle toggle) => toggle && toggle.gameObject.activeInHierarchy && toggle.isOn;
    }
}
