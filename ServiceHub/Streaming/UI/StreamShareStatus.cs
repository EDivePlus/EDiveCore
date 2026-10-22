// Author: František Holubec
// Created: 21.10.2026

using System;
using EDIVE.Core;
using EDIVE.Localization;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.ServiceHub.Streaming.Sources;
using EDIVE.View.Tooltips;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming.UI
{
    // Dock indicator while my view is shared
    public class StreamShareStatus : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private AToggleState _SharingToggle;

        [SerializeField]
        private TooltipTrigger _Tooltip;

        [SerializeField]
        [ShowIf(nameof(_Tooltip))]
        private StringVisualID _TooltipID;

        [Tooltip("Smart string, {0} is the viewer count")]
        [SerializeField]
        [ShowIf(nameof(_Tooltip))]
        private SafeLocalizedString _TooltipText = new();

        private IDisposable _registration;
        private ViewShare _viewShare;

        private void OnEnable()
        {
            Refresh();
            _registration = AppCore.Services.WhenRegistered<StreamingManager>(OnStreamingRegistered);
        }

        private void OnDisable()
        {
            _registration?.Dispose();
            _registration = null;
            Bind(null);
        }

        private void OnStreamingRegistered(StreamingManager streaming) => Bind(streaming.ViewShare);

        private void Bind(ViewShare viewShare)
        {
            if (_viewShare != null)
                _viewShare.Changed -= OnChanged;
            _viewShare = viewShare;
            if (_viewShare != null)
                _viewShare.Changed += OnChanged;
            Refresh();
        }

        private void OnChanged(ViewShare _) => Refresh();

        private void Refresh()
        {
            var sharing = _viewShare != null && _viewShare.IsSharing;
            var count = sharing ? _viewShare.Viewers.Count : 0;
            _SharingToggle.SetState(sharing);
            if (sharing && _Tooltip != null && _TooltipID != null)
                _Tooltip.SetPreset(new VisualPreset(new StringVisualPresetRecord(_TooltipID, _TooltipText.GetLocalizedString(count))));
        }
    }
}
