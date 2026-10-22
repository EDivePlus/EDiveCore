// Author: František Holubec
// Created: 21.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Localization;
using EDIVE.Networking.Players;
using EDIVE.ServiceHub;
using EDIVE.ServiceHub.Presence;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.ServiceHub.Streaming.Displays;
using EDIVE.ServiceHub.Streaming.Players;
using EDIVE.ServiceHub.Streaming.Sources;
using EDIVE.View.ViewTree;
using EDIVE.View.Windows;
using EDIVE.View.Windows.Screens;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Streaming.UI
{
    // Body of the Streams window: my view first, then people live and the others
    public class StreamsPanel : AViewBehaviour
    {
        [SerializeField]
        [Required]
        private WindowDefinition _WatchWindow;

        [SerializeField]
        [Required]
        private StreamPersonRow _RowPrefab;

        [SerializeField]
        private AToggleState _UnavailableToggle;

        [BoxGroup("My view")]
        [SerializeField]
        [Required]
        private AToggleState _SharingToggle;

        [BoxGroup("My view")]
        [SerializeField]
        private TMP_Text _MyViewInfo;

        [BoxGroup("My view")]
        [SerializeField]
        private SafeLocalizedString _NotSharedText = new();

        [BoxGroup("My view")]
        [SerializeField]
        private SafeLocalizedString _NobodyWatchingText = new();

        [Tooltip("Smart string, {0} is the list of viewers")]
        [BoxGroup("My view")]
        [SerializeField]
        private SafeLocalizedString _WatchedByText = new();

        [BoxGroup("My view")]
        [SerializeField]
        private Button _ShareButton;

        [BoxGroup("My view")]
        [SerializeField]
        private Button _StopButton;

        [BoxGroup("My view")]
        [SerializeField]
        private Toggle _MenusToggle;

        [BoxGroup("My view")]
        [SerializeField]
        private Toggle _HearingToggle;

        [BoxGroup("My view")]
        [SerializeField]
        private Toggle _MicToggle;

        [BoxGroup("People")]
        [SerializeField]
        private GameObject _LiveSection;

        [BoxGroup("People")]
        [SerializeField]
        [Required]
        private Transform _LiveContainer;

        [BoxGroup("People")]
        [SerializeField]
        private GameObject _OthersSection;

        [BoxGroup("People")]
        [SerializeField]
        [Required]
        private Transform _OthersContainer;

        [BoxGroup("People")]
        [SerializeField]
        private SafeLocalizedString _SharingViewText = new();

        [BoxGroup("People")]
        [SerializeField]
        private SafeLocalizedString _PresentingText = new();

        private readonly List<StreamPersonRow> _liveRows = new();
        private readonly List<StreamPersonRow> _otherRows = new();
        private StreamDirectory _directory;
        private bool _dirty;

        private StreamingManager Streaming => _directory?.Streaming;

        protected override void Awake()
        {
            base.Awake();
            if (_ShareButton) _ShareButton.onClick.AddListener(OnShareClicked);
            if (_StopButton) _StopButton.onClick.AddListener(OnStopClicked);
            if (_MenusToggle) _MenusToggle.onValueChanged.AddListener(OnMenusChanged);
            if (_HearingToggle) _HearingToggle.onValueChanged.AddListener(OnAudioChanged);
            if (_MicToggle) _MicToggle.onValueChanged.AddListener(OnAudioChanged);
        }

        protected override void OnDestroy()
        {
            if (_ShareButton) _ShareButton.onClick.RemoveListener(OnShareClicked);
            if (_StopButton) _StopButton.onClick.RemoveListener(OnStopClicked);
            if (_MenusToggle) _MenusToggle.onValueChanged.RemoveListener(OnMenusChanged);
            if (_HearingToggle) _HearingToggle.onValueChanged.RemoveListener(OnAudioChanged);
            if (_MicToggle) _MicToggle.onValueChanged.RemoveListener(OnAudioChanged);
            base.OnDestroy();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _directory = new StreamDirectory();
            _directory.Changed += MarkDirty;
            _directory.SetPolling(true);
            _dirty = true;
        }

        protected override void OnDisable()
        {
            _directory?.Dispose();
            _directory = null;
            base.OnDisable();
        }

        protected override void OnViewReset() => _directory?.Reset();

        private void LateUpdate()
        {
            if (!_dirty)
                return;
            _dirty = false;
            Refresh();
        }

        private void MarkDirty() => _dirty = true;

        private void Refresh()
        {
            var available = _directory != null && _directory.IsAvailable;
            if (_UnavailableToggle) _UnavailableToggle.SetState(!available);
            RefreshMyView();
            RefreshPeople();
        }

        private void RefreshMyView()
        {
            var share = Streaming?.ViewShare;
            var sharing = share != null && share.IsSharing;
            _SharingToggle.SetState(sharing);
            if (_ShareButton) _ShareButton.interactable = _directory != null && _directory.IsAvailable;
            if (_MenusToggle) _MenusToggle.SetIsOnWithoutNotify(share?.IncludeMenus ?? true);
            if (_HearingToggle) _HearingToggle.SetIsOnWithoutNotify(share != null && share.IncludeHearing);
            if (_MicToggle) _MicToggle.SetIsOnWithoutNotify(share != null && share.IncludeMic);

            if (!_MyViewInfo)
                return;
            if (!sharing)
                _MyViewInfo.text = _NotSharedText;
            else if (share.Viewers.Count == 0)
                _MyViewInfo.text = _NobodyWatchingText;
            else
                _MyViewInfo.text = _WatchedByText.GetLocalizedString(string.Join(", ", share.Viewers.Select(v => v.Name)));
        }

        private void RefreshPeople()
        {
            var live = GetLive();
            var others = _directory != null ? _directory.GetOthers() : new List<NetworkStreamPlayer>();

            Fill(_liveRows, _LiveContainer, live.Count, (row, i) =>
            {
                var item = live[i];
                var action = item.CanWatch ? StreamPersonRow.RowAction.Watch : GetAskAction(item.Identity);
                row.Set(item.Name, item.Info, action, item.CanWatch ? () => OpenWatch(item.Source) : () => Ask(item.Identity));
            });
            Fill(_otherRows, _OthersContainer, others.Count, (row, i) =>
            {
                var identity = others[i].State.Identity;
                row.Set(StreamDirectory.GetName(others[i]), null, GetAskAction(identity), () => Ask(identity));
            });

            if (_LiveSection) _LiveSection.SetActive(live.Count > 0);
            if (_OthersSection) _OthersSection.SetActive(others.Count > 0);
        }

        private StreamPersonRow.RowAction GetAskAction(string identity)
        {
            if (string.IsNullOrEmpty(identity) || _directory == null || !_directory.CanAskAnyone)
                return StreamPersonRow.RowAction.None;
            return _directory.IsAsked(identity) ? StreamPersonRow.RowAction.Waiting : StreamPersonRow.RowAction.Ask;
        }

        private List<StreamLiveItem> GetLive() => _directory != null ? _directory.GetLive(_SharingViewText, _PresentingText) : new List<StreamLiveItem>();

        private void Fill(List<StreamPersonRow> rows, Transform container, int count, Action<StreamPersonRow, int> apply)
        {
            while (rows.Count < count)
                rows.Add(Instantiate(_RowPrefab, container, false));
            for (var i = 0; i < rows.Count; i++)
            {
                var used = i < count;
                rows[i].gameObject.SetActive(used);
                if (!used)
                    continue;
                rows[i].SetAlternate(i % 2 == 1);
                apply(rows[i], i);
            }
        }

        private void OpenWatch(string source) => OpenWatchAsync(source).Forget();

        private async UniTaskVoid OpenWatchAsync(string source)
        {
            var screen = this.FindInViewTree<ScreenRoot>();
            if (screen == null || _WatchWindow == null)
                return;

            var entries = GetLive().Where(l => l.CanWatch).Select(l => new StreamEntry(l.Name, l.Source)).ToList();
            var index = Math.Max(0, entries.FindIndex(e => e.Source == source));
            if (entries.Count == 0)
                entries.Add(new StreamEntry(null, source));

            var handle = screen.Open(_WatchWindow);
            await handle.Opened;
            if (handle.Window != null)
                handle.Window.GetComponentInChildren<StreamWatchPanel>(true)?.Show(entries, index);
        }

        // Opens the watch window once their share reaches us
        private void Ask(string identity) => _directory?.AskAsync(identity, OpenWatch, destroyCancellationToken).Forget();

        private void OnShareClicked()
        {
            if (Streaming?.ViewShare != null)
                Streaming.ViewShare.StartAsync(destroyCancellationToken).Forget();
        }

        private void OnStopClicked() => Streaming?.ViewShare?.Stop();

        private void OnMenusChanged(bool isOn) => Streaming?.ViewShare?.SetMenus(isOn);

        private void OnAudioChanged(bool _)
        {
            Streaming?.ViewShare?.SetAudio(_HearingToggle && _HearingToggle.isOn, _MicToggle && _MicToggle.isOn);
        }
    }
}
