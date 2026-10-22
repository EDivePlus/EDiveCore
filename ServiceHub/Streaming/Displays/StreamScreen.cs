// Author: František Holubec
// Created: 20.10.2026

using System;
using EDIVE.Core;
using EDIVE.DataStructures.Identifiers;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Presence;
using EDIVE.StateHandling.MultiStates;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Streaming.Displays
{
    // A surface that shows a stream. Shared for the whole session with NetworkStreamScreen, else only for this player
    public class StreamScreen : MonoBehaviour
    {
        [SerializeField]
        [EnhancedBoxGroup("Display", Color = "@ColorTools.Cyan")]
        private UGuid _Id;

        [SerializeField]
        [EnhancedBoxGroup("Display")]
        private string _Name = "Display";

        [SerializeField]
        [EnhancedBoxGroup("Display")]
        private string _Place;

        [SerializeField]
        [EnhancedBoxGroup("Display")]
        [Tooltip("Who can show something here")]
        private StreamScreenAllow _Allow;

        [SerializeField]
        [EnhancedBoxGroup("Surface", Color = "@ColorTools.Green")]
        private Renderer _Renderer;

        [SerializeField]
        [EnhancedBoxGroup("Surface")]
        [ShowIf(nameof(_Renderer))]
        private string _TextureProperty = "_BaseMap";

        [SerializeField]
        [EnhancedBoxGroup("Surface")]
        private RawImage _RawImage;

        [SerializeField]
        [EnhancedBoxGroup("Surface")]
        [Tooltip("3D sound of the stream")]
        private AudioSource _AudioSource;

        [SerializeField]
        [EnhancedBoxGroup("View", Color = "@ColorTools.Orange")]
        [Tooltip("States Idle, Loading, Showing, Reconnecting")]
        private AMultiState _ViewState;

        [SerializeField]
        [EnhancedBoxGroup("View")]
        private TMP_Text _PresenterText;

        private NetworkStreamScreen _network;
        private StreamingManager _manager;
        private StreamFeed _feed;
        private MaterialPropertyBlock _propertyBlock;
        private int _textureId;
        private int _textureStId;
        private bool _showingLocal;

        public string Id => _Id.ToString();
        public string Name => _Name;
        public string Place => _Place;
        public StreamScreenAllow Allow => _Allow;
        public bool IsShared => _network != null;

        [ShowInInspector, ReadOnly]
        public StreamScreenState State { get; private set; } = StreamScreenState.Empty;

        [ShowInInspector, ReadOnly]
        public StreamScreenView View { get; private set; }

        public event Action<StreamScreen> StateChanged;

        private void Awake()
        {
            _network = GetComponent<NetworkStreamScreen>();
            _textureId = Shader.PropertyToID(_TextureProperty);
            _textureStId = Shader.PropertyToID(_TextureProperty + "_ST");
            ApplyView(StreamScreenView.Idle);
        }

        private void OnEnable()
        {
            if (AppCore.Services.TryGet(out _manager))
            {
                _manager.RegisterScreen(this);
                _manager.ViewShare.Changed += OnViewShareChanged;
            }
            RefreshFeed();
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.UnregisterScreen(this);
                _manager.ViewShare.Changed -= OnViewShareChanged;
            }
            _manager = null;
            ReleaseFeed();
            ClearLocal();
            ApplyView(StreamScreenView.Idle);
        }

        // Shared screens change through NetworkStreamScreen on the server
        public bool Show(StreamScreenState state)
        {
            if (IsShared)
                return _network.ServerSetState(state);
            SetState(state);
            return true;
        }

        internal void SetState(StreamScreenState state)
        {
            State = state ?? StreamScreenState.Empty;
            if (_PresenterText)
                _PresenterText.text = State.PresenterName ?? "";
            RefreshFeed();
            StateChanged?.Invoke(this);
        }

        public PresenceDisplay ToPresence() => new()
        {
            Id = Id,
            Name = _Name,
            Place = string.IsNullOrEmpty(_Place) ? null : _Place,
            Allow = _Allow switch
            {
                StreamScreenAllow.Managers => DisplayAllow.MANAGERS,
                StreamScreenAllow.Nobody => DisplayAllow.NOBODY,
                _ => DisplayAllow.ANYONE
            },
            Source = State.Source,
            Presenter = State.Presenter
        };

        private void OnViewShareChanged(Sources.ViewShare _) => RefreshFeed();

        private void RefreshFeed()
        {
            var source = isActiveAndEnabled && WantsVideo() ? State.Source : null;
            var share = _manager?.ViewShare;
            if (source != null && share != null && share.IsSharing && source == share.SourceId)
            {
                ReleaseFeed();
                _showingLocal = true;
                SetTexture(share.LocalTexture, share.LocalUpsideDown);
                ApplyView(StreamScreenView.Showing);
                return;
            }

            ClearLocal();
            if (_feed != null && _feed.SourceId == source)
                return;

            ReleaseFeed();
            if (source != null && _manager != null)
            {
                _feed = _manager.Room.Acquire(source);
                _feed.Changed += OnFeedChanged;
                _feed.AddAudioOutput(_AudioSource);
            }
            OnFeedChanged(_feed);
        }

        // A dedicated server only keeps the state
        private bool WantsVideo() => !IsShared || _network.isClient;

        private void ReleaseFeed()
        {
            if (_feed == null)
                return;
            _feed.Changed -= OnFeedChanged;
            _feed.RemoveAudioOutput(_AudioSource);
            if (_manager != null)
                _manager.Room.Release(_feed);
            _feed = null;
            SetTexture(null);
        }

        private void ClearLocal()
        {
            if (!_showingLocal)
                return;
            _showingLocal = false;
            SetTexture(null);
        }

        private void OnFeedChanged(StreamFeed feed)
        {
            if (feed == null)
            {
                ApplyView(StreamScreenView.Idle);
                return;
            }
            SetTexture(feed.Texture);
            ApplyView(GetFeedView(feed));
        }

        // Only display.clear from ServiceHub turns it idle
        private static StreamScreenView GetFeedView(StreamFeed feed) => feed.Status switch
        {
            StreamFeedStatus.Live => StreamScreenView.Showing,
            StreamFeedStatus.Reconnecting => StreamScreenView.Reconnecting,
            _ => StreamScreenView.Loading
        };

        private void ApplyView(StreamScreenView view)
        {
            View = view;
            if (_ViewState)
                _ViewState.SetState(view);
        }

        private void SetTexture(Texture texture, bool upsideDown = false)
        {
            if (_RawImage)
            {
                _RawImage.texture = texture;
                _RawImage.enabled = texture != null;
                _RawImage.uvRect = upsideDown ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
            }

            if (!_Renderer)
                return;
            _propertyBlock ??= new MaterialPropertyBlock();
            _Renderer.GetPropertyBlock(_propertyBlock);
            if (texture != null)
            {
                _propertyBlock.SetTexture(_textureId, texture);
                _propertyBlock.SetVector(_textureStId, upsideDown ? new Vector4(1, -1, 0, 1) : new Vector4(1, 1, 0, 0));
            }
            else
                _propertyBlock.Clear();
            _Renderer.SetPropertyBlock(_propertyBlock);
        }

#if UNITY_EDITOR
        private void Reset() => _Id = new UGuid(Guid.NewGuid());

        private void OnValidate()
        {
            if (_Id.IsEmpty)
                _Id = new UGuid(Guid.NewGuid());
        }
#endif
    }
}
