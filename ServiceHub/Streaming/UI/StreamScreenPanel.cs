// Author: František Holubec
// Created: 21.10.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Localization;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Presence;
using EDIVE.ServiceHub.Streaming.Displays;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.UIElements;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Streaming.UI
{
    // UI on the display itself: choose what it shows, turn it off, ask the presenter to hand over
    public class StreamScreenPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const string STATE_IDLE = "Idle";
        private const string STATE_PICKER = "Picker";
        private const string STATE_PLAYING = "Playing";

        // ServiceHub's handover countdown
        private const float HANDOVER_SECONDS = 20f;

        [SerializeField]
        [Required]
        private StreamScreen _Screen;

        [Tooltip("States Idle, Picker, Playing")]
        [SerializeField]
        [Required]
        private AMultiState _PanelState;

        [SerializeField]
        private TMP_Text[] _DisplayNameTexts = Array.Empty<TMP_Text>();

        [SerializeField]
        [EnhancedBoxGroup("Idle")]
        private Button _ChooseButton;

        [SerializeField]
        [EnhancedBoxGroup("Picker")]
        private Button _CloseButton;

        [SerializeField]
        [EnhancedBoxGroup("Picker")]
        private Button _TurnOffButton;

        [SerializeField]
        [EnhancedBoxGroup("Picker")]
        private Button _ShowMyViewButton;

        [SerializeField]
        [EnhancedBoxGroup("Picker")]
        [Required]
        private StreamPersonRow _RowPrefab;

        [SerializeField]
        [EnhancedBoxGroup("Picker")]
        [Required]
        private Transform _RowsContainer;

        [SerializeField]
        [EnhancedBoxGroup("Picker")]
        private SafeLocalizedString _SharingViewText = new();

        [SerializeField]
        [EnhancedBoxGroup("Picker")]
        private SafeLocalizedString _PresentingText = new();

        [SerializeField]
        [EnhancedBoxGroup("Picker")]
        private SafeLocalizedString _NotSharingText = new();

        [Tooltip("Shown while pointing at the display or waiting for a handover")]
        [SerializeField]
        [EnhancedBoxGroup("Bar")]
        private AToggleState _BarToggle;

        [SerializeField]
        [EnhancedBoxGroup("Bar")]
        private ProfilePictureDisplay _PresenterAvatar;

        [SerializeField]
        [EnhancedBoxGroup("Bar")]
        private TMP_Text _PresenterText;

        [Tooltip("On mutes the display")]
        [SerializeField]
        [EnhancedBoxGroup("Bar")]
        private Toggle _MuteToggle;

        [SerializeField]
        [EnhancedBoxGroup("Bar")]
        private AudioSource _AudioSource;

        [SerializeField]
        [EnhancedBoxGroup("Bar")]
        private Button _AskButton;

        [SerializeField]
        [EnhancedBoxGroup("Waiting")]
        private AToggleState _WaitingToggle;

        [SerializeField]
        [EnhancedBoxGroup("Waiting")]
        private TMP_Text _WaitingText;

        [Tooltip("Smart string, {0} is the seconds left")]
        [SerializeField]
        [EnhancedBoxGroup("Waiting")]
        private SafeLocalizedString _WaitingFormat = new();

        [SerializeField]
        [EnhancedBoxGroup("Waiting")]
        private Button _CancelButton;

        [SerializeField]
        [EnhancedBoxGroup("Message")]
        private GameObject _Message;

        [SerializeField]
        [EnhancedBoxGroup("Message")]
        private TMP_Text _MessageText;

        [Tooltip("Smart string, {0} is the presenter")]
        [SerializeField]
        [EnhancedBoxGroup("Message")]
        private SafeLocalizedString _KeptText = new();

        [SerializeField]
        [EnhancedBoxGroup("Message")]
        private SafeLocalizedString _FailedText = new();

        [SerializeField]
        [EnhancedBoxGroup("Message")]
        private float _MessageDuration = 4f;

        private readonly List<StreamPersonRow> _rows = new();
        private StreamDirectory _directory;
        private CancellationTokenSource _request;
        private bool _pickerOpen;
        private bool _pointerOver;
        private float _waitingUntil = -1f;
        private float _messageUntil = -1f;
        private int _shownSeconds = -1;
        private bool _dirty;

        private StreamingManager Streaming => _directory?.Streaming;
        private bool IsWaiting => _waitingUntil > 0f;

        private void Awake()
        {
            if (_ChooseButton) _ChooseButton.onClick.AddListener(OpenPicker);
            if (_CloseButton) _CloseButton.onClick.AddListener(ClosePicker);
            if (_TurnOffButton) _TurnOffButton.onClick.AddListener(OnTurnOffClicked);
            if (_ShowMyViewButton) _ShowMyViewButton.onClick.AddListener(OnShowMyViewClicked);
            if (_AskButton) _AskButton.onClick.AddListener(OnAskClicked);
            if (_CancelButton) _CancelButton.onClick.AddListener(CancelRequest);
            if (_MuteToggle) _MuteToggle.onValueChanged.AddListener(OnMuteChanged);
        }

        private void OnDestroy()
        {
            if (_ChooseButton) _ChooseButton.onClick.RemoveListener(OpenPicker);
            if (_CloseButton) _CloseButton.onClick.RemoveListener(ClosePicker);
            if (_TurnOffButton) _TurnOffButton.onClick.RemoveListener(OnTurnOffClicked);
            if (_ShowMyViewButton) _ShowMyViewButton.onClick.RemoveListener(OnShowMyViewClicked);
            if (_AskButton) _AskButton.onClick.RemoveListener(OnAskClicked);
            if (_CancelButton) _CancelButton.onClick.RemoveListener(CancelRequest);
            if (_MuteToggle) _MuteToggle.onValueChanged.RemoveListener(OnMuteChanged);
        }

        private void OnEnable()
        {
            _directory = new StreamDirectory();
            _directory.Changed += MarkDirty;
            _Screen.StateChanged += OnScreenChanged;
            foreach (var text in _DisplayNameTexts)
            {
                if (text) text.text = _Screen.Name;
            }
            if (_Message) _Message.SetActive(false);
            _dirty = true;
        }

        private void OnDisable()
        {
            CancelRequest();
            _Screen.StateChanged -= OnScreenChanged;
            _directory?.Dispose();
            _directory = null;
            _pickerOpen = false;
            _pointerOver = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerOver = true;
            _dirty = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerOver = false;
            _dirty = true;
        }

        private void Update()
        {
            if (IsWaiting)
                RefreshWaiting();
            if (_messageUntil > 0f && Time.realtimeSinceStartup >= _messageUntil)
            {
                _messageUntil = -1f;
                if (_Message) _Message.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (!_dirty)
                return;
            _dirty = false;
            Refresh();
        }

        private void MarkDirty() => _dirty = true;

        private void OnScreenChanged(StreamScreen _) => _dirty = true;

        private void Refresh()
        {
            var state = _Screen.State;
            var playing = !state.IsEmpty;
            _PanelState.SetState(_pickerOpen ? STATE_PICKER : playing ? STATE_PLAYING : STATE_IDLE);
            _directory?.SetPolling(_pickerOpen);

            if (_BarToggle) _BarToggle.SetState(playing && !_pickerOpen && (_pointerOver || IsWaiting));
            if (_WaitingToggle) _WaitingToggle.SetState(IsWaiting);
            if (_PresenterAvatar) _PresenterAvatar.SetProfilePictureFromName(string.IsNullOrEmpty(state.PresenterName) ? "?" : state.PresenterName);
            if (_PresenterText) _PresenterText.text = state.PresenterName;
            if (_AskButton) _AskButton.gameObject.SetActive(!IsWaiting && !IsMine(state));
            if (_TurnOffButton) _TurnOffButton.interactable = playing && !IsWaiting;
            if (_ShowMyViewButton) _ShowMyViewButton.interactable = _directory != null && _directory.IsAvailable && !IsWaiting;
            if (_MuteToggle && _AudioSource) _MuteToggle.SetIsOnWithoutNotify(_AudioSource.mute);

            if (_pickerOpen)
                RefreshRows();
        }

        private void RefreshRows()
        {
            var live = _directory != null ? _directory.GetLive(_SharingViewText, _PresentingText) : new List<StreamLiveItem>();
            var others = _directory != null ? _directory.GetOthers() : new List<Players.NetworkStreamPlayer>();
            var count = live.Count + others.Count;

            while (_rows.Count < count)
                _rows.Add(Instantiate(_RowPrefab, _RowsContainer, false));
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                row.gameObject.SetActive(i < count);
                if (i < live.Count)
                {
                    var item = live[i];
                    if (item.CanWatch)
                        row.Set(item.Name, item.Info, StreamPersonRow.RowAction.Watch, () => Request(item.Source));
                    else
                        row.Set(item.Name, item.Info, GetAskAction(item.Identity), () => AskThenShow(item.Identity));
                }
                else if (i < count)
                {
                    var player = others[i - live.Count];
                    var identity = player.State.Identity;
                    row.Set(StreamDirectory.GetName(player), _NotSharingText, GetAskAction(identity), () => AskThenShow(identity));
                }
            }
        }

        private StreamPersonRow.RowAction GetAskAction(string identity)
        {
            if (string.IsNullOrEmpty(identity) || _directory == null || !_directory.CanAskAnyone)
                return StreamPersonRow.RowAction.None;
            return _directory.IsAsked(identity) ? StreamPersonRow.RowAction.Waiting : StreamPersonRow.RowAction.Ask;
        }

        private bool IsMine(StreamScreenState state)
        {
            var share = Streaming?.ViewShare;
            return share != null && share.IsSharing && state.Source == share.SourceId;
        }

        private void OpenPicker()
        {
            _pickerOpen = true;
            _dirty = true;
        }

        private void ClosePicker()
        {
            _pickerOpen = false;
            _dirty = true;
        }

        private void OnTurnOffClicked() => Request(null);

        private void OnShowMyViewClicked() => RunRequest(token => Streaming.ShowMyViewAsync(_Screen, token), false).Forget();

        // Turns it off after the presenter hands over, then the picker opens
        private void OnAskClicked()
        {
            if (Streaming != null)
                RunRequest(token => Streaming.SetDisplayAsync(_Screen, null, token), true).Forget();
        }

        private void AskThenShow(string identity)
        {
            _directory?.AskAsync(identity, Request, destroyCancellationToken).Forget();
        }

        private void Request(string source)
        {
            if (Streaming != null)
                RunRequest(token => Streaming.SetDisplayAsync(_Screen, source, token), false).Forget();
        }

        private async UniTaskVoid RunRequest(Func<CancellationToken, UniTask<StreamActionResponse>> send, bool pickAfter)
        {
            if (_request != null || Streaming == null)
                return;

            _request = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var state = _Screen.State;
            var presenter = state.PresenterName;
            // Someone else's stream: ServiceHub asks them first
            if (!state.IsEmpty && !IsMine(state))
                StartWaiting();

            StreamActionResponse response;
            try
            {
                response = await send(_request.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _request?.Dispose();
                _request = null;
                StopWaiting();
            }

            if (response.Result == PresenceResult.APPLIED)
            {
                _pickerOpen = pickAfter;
                _dirty = true;
                return;
            }
            if (response.Result == PresenceResult.REFUSED)
                ShowMessage(string.IsNullOrEmpty(response.Message) ? _KeptText.GetLocalizedString(presenter) : response.Message);
            else
                ShowMessage(_FailedText);
        }

        private void CancelRequest()
        {
            _request?.Cancel();
            StopWaiting();
        }

        private void StartWaiting()
        {
            _waitingUntil = Time.realtimeSinceStartup + HANDOVER_SECONDS;
            _shownSeconds = -1;
            RefreshWaiting();
            _dirty = true;
        }

        private void StopWaiting()
        {
            _waitingUntil = -1f;
            _dirty = true;
        }

        private void RefreshWaiting()
        {
            var seconds = Mathf.Max(0, Mathf.CeilToInt(_waitingUntil - Time.realtimeSinceStartup));
            if (seconds == _shownSeconds || !_WaitingText)
                return;
            _shownSeconds = seconds;
            _WaitingText.text = _WaitingFormat.GetLocalizedString(seconds);
        }

        private void ShowMessage(string text)
        {
            if (!_Message || !_MessageText)
                return;
            _MessageText.text = text;
            _Message.SetActive(true);
            _messageUntil = Time.realtimeSinceStartup + _MessageDuration;
        }

        private void OnMuteChanged(bool muted)
        {
            if (_AudioSource) _AudioSource.mute = muted;
        }
    }
}
