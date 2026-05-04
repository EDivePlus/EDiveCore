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
        private string _ContentId;

        [SerializeField]
        private Transform _ContentRoot;

        [PropertySpace]
        [SerializeField]
        private RemoteContentState _State = RemoteContentState.Unknown;

        [SerializeField]
        [ValidateMultiState(typeof(RemoteContentState))]
        private AMultiState _VisualState;

        public RemoteContentState State
        {
            get => _State;
            private set
            {
                if (_State == value)
                    return;

                _State = value;
                if (_VisualState != null)
                    _VisualState.SetState(_State);
                OnStateChanged?.Invoke(_State);
            }
        }

        public event Action<RemoteContentState> OnStateChanged;

        protected virtual void Start()
        {
            LoadContentAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask LoadContentAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_ContentId))
            {
                State = RemoteContentState.Error;
                return;
            }

            State = RemoteContentState.Loading;

            var response = await AppCore.Services.Get<ServiceHubManager>().GetRemoteContentAsync(_ContentId, cancellationToken);
            if (!response.IsSuccess)
            {
                Debug.LogError($"[RemoteContent] Failed to fetch '{_ContentId}': {response.ErrorMessage}");
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
                Debug.LogError($"[RemoteContent] Failed to apply '{_ContentId}': {e}");
                State = RemoteContentState.Error;
            }
        }

        protected abstract UniTask ApplyContentAsync(RemoteContentResult content, CancellationToken cancellationToken);
    }

    public enum RemoteContentState
    {
        Loading,
        Ready,
        Error,
        Unknown
    }
}
