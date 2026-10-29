// Author: František Holubec
// Created: 21.10.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Localization;
using EDIVE.ServiceHub.Streaming.Displays;
using EDIVE.ServiceHub.Streaming.Sources;
using EDIVE.UIElements;
using EDIVE.Utils.Actions;
using EDIVE.View.Toasts;
using EDIVE.View.ViewTree;
using EDIVE.View.Windows;
using EDIVE.View.Windows.Screens;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming.UI
{
    // Opens the watch request and display handover dialogs, shows toasts for new viewers and ServiceHub notices. Lives in the menu view tree
    public class StreamShareNotifier : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private WindowDefinition _RequestWindow;

        [SerializeField]
        [Required]
        private WindowDefinition _HandoverWindow;

        [SerializeReference]
        [Tooltip("Runs before the request window opens, e.g. opens the tablet")]
        private IAction _RequestAction;

        [SerializeField]
        [Required]
        private ToastDisplay _WatchingToast;

        [SerializeField]
        private float _ToastDuration = 4f;

        [Tooltip("Icon and colors of the toast")]
        [SerializeField]
        private VisualPreset _WatchingPreset = new();

        [SerializeField]
        [Required]
        private StringVisualID _TitleID;

        [SerializeField]
        [Required]
        private StringVisualID _TextID;

        [Tooltip("Viewer avatar in the watching toast")]
        [SerializeField]
        private StringVisualID _InitialsID;

        [SerializeField]
        private ColorVisualID _AvatarColorID;

        [Tooltip("Smart string, {0} is the viewer name")]
        [SerializeField]
        private SafeLocalizedString _WatchingTitle = new();

        [SerializeField]
        private SafeLocalizedString _WebText = new();

        [SerializeField]
        private SafeLocalizedString _AppText = new();

        [Tooltip("Icon and colors of notice toasts, e.g. a display taken over")]
        [SerializeField]
        private VisualPreset _NoticePreset = new();

        private readonly HashSet<string> _knownViewers = new();
        private readonly List<ToastHandle> _toasts = new();
        private IDisposable _registration;
        private StreamingManager _streaming;
        private ViewShare _viewShare;
        private ScreenRoot _screen;

        private void OnEnable()
        {
            _screen = this.FindInViewTree<ScreenRoot>();
            if (_screen == null)
                Debug.LogWarning($"[StreamShareNotifier] {name} is not under a ScreenRoot.", this);
            _registration = AppCore.Services.WhenRegistered<StreamingManager>(OnStreamingRegistered);
        }

        private void OnDisable()
        {
            _registration?.Dispose();
            _registration = null;
            BindStreaming(null);
        }

        private void OnStreamingRegistered(StreamingManager streaming) => BindStreaming(streaming);

        private void BindStreaming(StreamingManager streaming)
        {
            if (_streaming != null)
            {
                _streaming.HandoverRequested -= OnHandoverRequested;
                _streaming.NoticeReceived -= OnNoticeReceived;
            }
            _streaming = streaming;
            if (_streaming != null)
            {
                _streaming.HandoverRequested += OnHandoverRequested;
                _streaming.NoticeReceived += OnNoticeReceived;
            }
            Bind(_streaming?.ViewShare);
        }

        private void OnHandoverRequested(DisplayHandoverRequest request)
        {
            // No dialog, the countdown hands it over
            if (_screen != null && _HandoverWindow != null)
                _screen.Open(_HandoverWindow, request);
        }

        private void OnNoticeReceived(string text, string kind)
        {
            var toasts = this.FindInViewTree<ToastManager>();
            if (toasts == null || string.IsNullOrEmpty(text))
                return;
            toasts.Show(_NoticePreset.CombineWith(new VisualPreset(
                new StringVisualPresetRecord(_TitleID, text),
                new StringVisualPresetRecord(_TextID, string.Empty))), _ToastDuration);
        }

        private void Bind(ViewShare viewShare)
        {
            if (_viewShare != null)
            {
                _viewShare.Requested -= OnRequested;
                _viewShare.Changed -= OnChanged;
            }
            _viewShare = viewShare;
            _knownViewers.Clear();
            DismissToasts();
            if (_viewShare == null)
                return;
            _viewShare.Requested += OnRequested;
            _viewShare.Changed += OnChanged;
            OnChanged(_viewShare);
        }

        private void OnRequested(StreamShareRequest request)
        {
            if (_screen == null || _RequestWindow == null)
            {
                request.Deny();
                return;
            }
            _RequestAction?.Execute().Forget();
            _screen.Open(_RequestWindow, request);
        }

        private void OnChanged(ViewShare viewShare)
        {
            if (!viewShare.IsSharing)
            {
                _knownViewers.Clear();
                DismissToasts();
                return;
            }

            var current = new HashSet<string>();
            foreach (var viewer in viewShare.Viewers)
            {
                current.Add(viewer.Identity);
                if (!_knownViewers.Contains(viewer.Identity))
                    ShowToast(viewer);
            }
            _knownViewers.Clear();
            _knownViewers.UnionWith(current);
        }

        private void ShowToast(StreamViewer viewer)
        {
            var toasts = this.FindInViewTree<ToastManager>();
            if (toasts == null || _WatchingToast == null)
                return;

            var preset = _WatchingPreset.CombineWith(new VisualPreset(
                new StringVisualPresetRecord(_TitleID, _WatchingTitle.GetLocalizedString(viewer.Name)),
                new StringVisualPresetRecord(_TextID, viewer.IsWeb ? _WebText : _AppText)));
            if (_InitialsID != null)
                preset = preset.CombineWith(new VisualPreset(new StringVisualPresetRecord(_InitialsID, ProfilePictureDisplay.GetInitials(viewer.Name))));
            if (_AvatarColorID != null)
                preset = preset.CombineWith(new VisualPreset(new ColorVisualPresetRecord(_AvatarColorID, ProfilePictureDisplay.ColorFromString(viewer.Name))));
            _toasts.RemoveAll(t => t.IsDismissed);
            _toasts.Add(toasts.Show(preset, _ToastDuration, _WatchingToast));
        }

        private void DismissToasts()
        {
            foreach (var toast in _toasts)
                toast.Dismiss();
            _toasts.Clear();
        }
    }
}
