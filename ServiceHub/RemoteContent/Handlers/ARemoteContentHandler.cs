// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.ServiceHub.RemoteContent.Handlers
{
    public abstract class ARemoteContentHandler : MonoBehaviour
    {
        [SerializeField]
        [ValidateMultiState(typeof(RemoteContentState))]
        private AMultiState _VisualState;

        [SerializeField]
        private string _ShareToken;
        public string ShareToken => _ShareToken;
        
        [SerializeField]
        private XRBaseInteractable _Interactable;

        [PropertySpace]
        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(999)]
        private RemoteContentState _state = RemoteContentState.Unknown;
        
        private CancellationTokenSource _loadCts;
        
        private bool _loadInitialized;

        public ContentItemInfo ContentInfo { get; private set; }
        
        public RemoteContentState State
        {
            get => _state;
            private set
            {
                if (_state == value)
                    return;

                _state = value;
                if (_VisualState != null)
                    _VisualState.SetState(_state);
                StateChanged?.Invoke(_state);
            }
        }

        public event Action<RemoteContentState> StateChanged;
        public event Action<string> ShareTokenChanged;
        
        public abstract bool IsValidFor(ContentItemInfo contentInfo);

        public void SetShareToken(string shareToken)
        {
            if (_ShareToken == shareToken)
                return;
            _ShareToken = shareToken;
            
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;
            
            ShareTokenChanged?.Invoke(_ShareToken);
            TryStartLoad();
        }

        protected virtual void Start()
        {
            TryStartLoad();
        }

        protected virtual void OnDestroy()
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;
        }

        private void OnEnable()
        {
            if (_Interactable != null)
                _Interactable.activated.AddListener(OnInteractableActivated);
        }

        private void OnDisable()
        {
            if (_Interactable != null)
                _Interactable.activated.RemoveListener(OnInteractableActivated);
        }

        private void TryStartLoad()
        {
            if (string.IsNullOrEmpty(_ShareToken) || _loadInitialized)
                return;
            _loadInitialized = true;
            _loadCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            LoadContentAsync(_ShareToken, _loadCts.Token).Forget();
        }

        private async UniTask LoadContentAsync(string shareToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(shareToken))
            {
                State = RemoteContentState.Error;
                return;
            }

            State = RemoteContentState.Loading;

            var contentApi = AppCore.Services.Get<ServiceHubManager>().RemoteContent;

            var infoResponse = await contentApi.GetSharedContentInfoAsync(shareToken, cancellationToken);
            if (!infoResponse.IsSuccess || infoResponse.Result == null)
            {
                Debug.LogError($"[RemoteContent] Failed to fetch info for token '{shareToken}': {infoResponse.ErrorMessage}");
                State = RemoteContentState.Error;
                return;
            }
            ContentInfo = infoResponse.Result;

            var response = await contentApi.GetRemoteContentAsync(shareToken, cancellationToken);
            if (!response.IsSuccess)
            {
                Debug.LogError($"[RemoteContent] Failed to fetch token '{shareToken}': {response.ErrorMessage}");
                State = RemoteContentState.Error;
                return;
            }

            try
            {
                await ApplyContentAsync(response.Result, cancellationToken);
                State = RemoteContentState.Ready;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RemoteContent] Failed to apply token '{shareToken}': {e}");
                State = RemoteContentState.Error;
            }
        }

        protected abstract UniTask ApplyContentAsync(RemoteContentResult content, CancellationToken cancellationToken);
        
        private void OnInteractableActivated(ActivateEventArgs args)
        {
            if (AppCore.Services.TryGet<RemoteContentManager>(out var manager))
            {
                manager.RequestHandlerSelected(this);
            }
        }
    }

    public enum RemoteContentState
    {
        Initial,
        Loading,
        Ready,
        Error,
        Unknown
    }
}
