// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent.Handlers
{
    public abstract class ARemoteContentHandler : NetworkBehaviour
    {
        [SerializeField]
        [ValidateMultiState(typeof(RemoteContentState))]
        private AMultiState _VisualState;

        [PropertySpace]
        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(999)]
        private RemoteContentState _state = RemoteContentState.Unknown;

        private readonly SyncVar<string> _contentId = new();
        private bool _loadStarted;

        public ContentItemInfo ContentInfo { get; private set; }

        public abstract bool IsValidFor(ContentItemInfo contentInfo);

        public void ServerSetContentId(string contentId)
        {
            _contentId.Value = contentId;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _contentId.OnChange += OnContentIdChanged;
        }

        public override void OnStopNetwork()
        {
            _contentId.OnChange -= OnContentIdChanged;
            base.OnStopNetwork();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            TryStartLoad();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            TryStartLoad();
        }

        private void OnContentIdChanged(string prev, string next, bool asServer)
        {
            TryStartLoad();
        }

        private void TryStartLoad()
        {
            if (_loadStarted)
                return;
            var id = _contentId.Value;
            if (string.IsNullOrEmpty(id))
                return;
            _loadStarted = true;
            LoadContentAsync(id, this.GetCancellationTokenOnDestroy()).Forget();
        }

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
                OnStateChanged?.Invoke(_state);
            }
        }

        public event Action<RemoteContentState> OnStateChanged;

        private async UniTask LoadContentAsync(string contentId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(contentId))
            {
                State = RemoteContentState.Error;
                return;
            }

            State = RemoteContentState.Loading;

            var serviceHub = AppCore.Services.Get<ServiceHubManager>();
            var shareResponse = await serviceHub.CreateContentShareAsync(contentId, cancellationToken);
            if (!shareResponse.IsSuccess || shareResponse.Result == null || string.IsNullOrEmpty(shareResponse.Result.Token))
            {
                Debug.LogError($"[RemoteContent] Failed to create share for '{contentId}': {shareResponse.ErrorMessage}");
                State = RemoteContentState.Error;
                return;
            }

            if (shareResponse.Result.Item != null)
                ContentInfo = shareResponse.Result.Item;

            var response = await serviceHub.GetRemoteContentAsync(shareResponse.Result.Token, cancellationToken);
            if (!response.IsSuccess)
            {
                Debug.LogError($"[RemoteContent] Failed to fetch '{contentId}': {response.ErrorMessage}");
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
                Debug.LogError($"[RemoteContent] Failed to apply '{contentId}': {e}");
                State = RemoteContentState.Error;
            }
        }

        protected abstract UniTask ApplyContentAsync(RemoteContentResult content, CancellationToken cancellationToken);
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
