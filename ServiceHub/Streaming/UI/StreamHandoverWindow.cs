// Author: František Holubec
// Created: 21.10.2026

using System;
using EDIVE.Localization;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Streaming.Displays;
using EDIVE.UIElements;
using EDIVE.UIElements.SDFShapes;
using EDIVE.View.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Streaming.UI
{
    // Someone wants the display I present on: Keep or Hand over before ServiceHub's countdown ends
    public class StreamHandoverWindow : AWindow<DisplayHandoverRequest, bool>
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

        [Tooltip("Smart string, {0} is the display")]
        [SerializeField]
        [EnhancedBoxGroup("Request")]
        private SafeLocalizedString _AppInfo = new();

        [Tooltip("Smart string, {0} is the display")]
        [SerializeField]
        [EnhancedBoxGroup("Request")]
        private SafeLocalizedString _WebInfo = new();

        [SerializeField]
        [EnhancedBoxGroup("Request")]
        private SafeLocalizedString _UnknownDisplay = new();

        [SerializeField]
        [EnhancedBoxGroup("Countdown")]
        private SDFArc _CountdownArc;

        [SerializeField]
        [EnhancedBoxGroup("Countdown")]
        private TMP_Text _HandOverLabel;

        [Tooltip("Smart string, {0} is the seconds left")]
        [SerializeField]
        [EnhancedBoxGroup("Countdown")]
        private SafeLocalizedString _HandOverText = new();

        [SerializeField]
        [EnhancedBoxGroup("Buttons")]
        private Button _KeepButton;

        [SerializeField]
        [EnhancedBoxGroup("Buttons")]
        private Button _HandOverButton;

        private DisplayHandoverRequest _request;
        private double _duration;
        private int _shownSeconds = -1;

        protected override void Awake()
        {
            base.Awake();
            if (_KeepButton) _KeepButton.onClick.AddListener(OnKeepClicked);
            if (_HandOverButton) _HandOverButton.onClick.AddListener(OnHandOverClicked);
        }

        protected override void OnDestroy()
        {
            if (_KeepButton) _KeepButton.onClick.RemoveListener(OnKeepClicked);
            if (_HandOverButton) _HandOverButton.onClick.RemoveListener(OnHandOverClicked);
            Unbind();
            base.OnDestroy();
        }

        protected override void OnContextSet(DisplayHandoverRequest request)
        {
            Unbind();
            _request = request;
            if (_request == null)
            {
                Debug.LogError($"[StreamHandoverWindow] {name} opened without a request.", this);
                return;
            }

            _request.Closed += OnRequestClosed;
            _duration = Math.Max(1, _request.Remaining.TotalSeconds);
            _shownSeconds = -1;

            var display = string.IsNullOrEmpty(_request.DisplayName) ? _UnknownDisplay.ToString() : _request.DisplayName;
            if (_Avatar) _Avatar.SetProfilePictureFromName(_request.ByName);
            if (_NameText) _NameText.text = _request.ByName;
            if (_InfoText) _InfoText.text = (_request.ByWeb ? _WebInfo : _AppInfo).GetLocalizedString(display);
            RefreshCountdown();
        }

        protected override void OnWindowClosed()
        {
            // Closed without an answer hands it over, same as the timeout
            _request?.HandOver();
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
            if (seconds == _shownSeconds || !_HandOverLabel)
                return;
            _shownSeconds = seconds;
            _HandOverLabel.text = _HandOverText.GetLocalizedString(seconds);
        }

        private void OnKeepClicked()
        {
            _request?.Keep();
            Close(true);
        }

        private void OnHandOverClicked()
        {
            _request?.HandOver();
            Close(false);
        }

        private void OnRequestClosed(DisplayHandoverRequest _)
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
    }
}
