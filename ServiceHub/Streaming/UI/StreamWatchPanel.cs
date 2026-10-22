// Author: František Holubec
// Created: 21.10.2026

using System.Collections.Generic;
using EDIVE.Core;
using EDIVE.Localization;
using EDIVE.View.ViewTree;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Streaming.UI
{
    // Body of the watch window: one live source at a time, previous and next go through the live list
    public class StreamWatchPanel : AViewBehaviour
    {
        [SerializeField]
        private RawImage _Video;

        [SerializeField]
        private GameObject _Loading;

        [Tooltip("Shown over the dimmed last frame while the source is gone")]
        [SerializeField]
        private GameObject _Reconnecting;

        [SerializeField]
        private Color _DimmedColor = new(0.4f, 0.4f, 0.4f, 1f);

        [SerializeField]
        private TMP_Text _NameText;

        [SerializeField]
        private TMP_Text _PositionText;

        [Tooltip("Smart string, {0} is the position, {1} the live count")]
        [SerializeField]
        private SafeLocalizedString _PositionFormat = new();

        [SerializeField]
        private Button _PreviousButton;

        [SerializeField]
        private Button _NextButton;

        private readonly List<StreamEntry> _entries = new();
        private int _index;
        private StreamFeed _feed;
        private StreamingManager _streaming;
        private Color _videoColor = Color.white;
        private bool _reconnecting;

        protected override void Awake()
        {
            base.Awake();
            if (_Video) _videoColor = _Video.color;
            if (_PreviousButton) _PreviousButton.onClick.AddListener(OnPrevious);
            if (_NextButton) _NextButton.onClick.AddListener(OnNext);
        }

        protected override void OnDestroy()
        {
            if (_PreviousButton) _PreviousButton.onClick.RemoveListener(OnPrevious);
            if (_NextButton) _NextButton.onClick.RemoveListener(OnNext);
            ReleaseFeed();
            base.OnDestroy();
        }

        protected override void OnDisable()
        {
            ReleaseFeed();
            base.OnDisable();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        protected override void OnViewReset()
        {
            _entries.Clear();
            _index = 0;
            ReleaseFeed();
        }

        public void Show(IEnumerable<StreamEntry> entries, int index)
        {
            _entries.Clear();
            _entries.AddRange(entries);
            _index = _entries.Count > 0 ? Mathf.Clamp(index, 0, _entries.Count - 1) : 0;
            Refresh();
        }

        private void OnPrevious() => Step(-1);
        private void OnNext() => Step(1);

        private void Step(int delta)
        {
            if (_entries.Count < 2)
                return;
            _index = (_index + delta + _entries.Count) % _entries.Count;
            Refresh();
        }

        private void Refresh()
        {
            var hasEntry = _index < _entries.Count;
            var entry = hasEntry ? _entries[_index] : default;
            if (_NameText) _NameText.text = entry.Name;
            if (_PositionText) _PositionText.text = _entries.Count > 1 ? _PositionFormat.GetLocalizedString(_index + 1, _entries.Count) : string.Empty;
            if (_PreviousButton) _PreviousButton.interactable = _entries.Count > 1;
            if (_NextButton) _NextButton.interactable = _entries.Count > 1;

            if (!isActiveAndEnabled || !hasEntry)
            {
                ReleaseFeed();
                ShowTexture(null);
                return;
            }
            if (_feed != null && _feed.SourceId == entry.Source)
                return;

            ReleaseFeed();
            if (_streaming == null && !AppCore.Services.TryGet(out _streaming))
                return;
            _feed = _streaming.Room.Acquire(entry.Source);
            _feed.Changed += OnFeedChanged;
            OnFeedChanged(_feed);
        }

        private void ReleaseFeed()
        {
            if (_feed == null)
                return;
            _feed.Changed -= OnFeedChanged;
            _streaming?.Room?.Release(_feed);
            _feed = null;
        }

        private void OnFeedChanged(StreamFeed feed) => ShowTexture(feed.Texture);

        private void RefreshReconnecting()
        {
            var reconnecting = _feed != null && _feed.Status == StreamFeedStatus.Reconnecting;
            if (reconnecting == _reconnecting)
                return;
            _reconnecting = reconnecting;
            if (_Video) _Video.color = reconnecting ? _DimmedColor : _videoColor;
            if (_Reconnecting) _Reconnecting.SetActive(reconnecting);
        }

        private void ShowTexture(Texture texture)
        {
            if (_Video)
            {
                _Video.texture = texture;
                _Video.enabled = texture != null;
            }
            if (_Loading) _Loading.SetActive(texture == null && _feed != null);
            RefreshReconnecting();
        }
    }
}
