// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent
{
    public abstract class ARemoteContentHandler : MonoBehaviour
    {
        [SerializeField]
        [ValidateMultiState(typeof(RemoteContentState))]
        private AMultiState _VisualState;
        
        [PropertySpace]
        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(999)]
        private RemoteContentState _state = RemoteContentState.Unknown;

        public ContentItemInfo ContentInfo { get; private set; }

        public virtual void Initialize(ContentItemInfo contentInfo)
        {
            ContentInfo = contentInfo;
            LoadContentAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
        
        public abstract bool IsValidFor(ContentItemInfo contentInfo);

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

        private async UniTask LoadContentAsync(CancellationToken cancellationToken = default)
        {
            if (ContentInfo == null || string.IsNullOrEmpty(ContentInfo.Id))
            {
                State = RemoteContentState.Error;
                return;
            }

            State = RemoteContentState.Loading;

            var serviceHub = AppCore.Services.Get<ServiceHubManager>();
            var shareResponse = await serviceHub.CreateContentShareAsync(ContentInfo.Id, cancellationToken);
            if (!shareResponse.IsSuccess || shareResponse.Result == null || string.IsNullOrEmpty(shareResponse.Result.Token))
            {
                Debug.LogError($"[RemoteContent] Failed to create share for '{ContentInfo.Id}': {shareResponse.ErrorMessage}");
                State = RemoteContentState.Error;
                return;
            }

            var response = await serviceHub.GetRemoteContentAsync(shareResponse.Result.Token, cancellationToken);
            if (!response.IsSuccess)
            {
                Debug.LogError($"[RemoteContent] Failed to fetch '{ContentInfo.Id}': {response.ErrorMessage}");
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
                Debug.LogError($"[RemoteContent] Failed to apply '{ContentInfo.Id}': {e}");
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
