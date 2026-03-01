// Author: František Holubec
// Created: 01.03.2026

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EDIVE.Core.Services;
using EDIVE.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.Loading
{
    public class LoadingOverlayProvider : AServiceBehaviour<LoadingOverlayProvider>
    {
        [SerializeField]
        private TweenAnimationField _FadeIn;
        
        [SerializeField]
        private TweenAnimationField _FadeOut;
        
        private readonly HashSet<object> _requesters = new();

        public void RequestOverlay(object requester)
        {
            if (requester == null)
                return;

            if (_requesters.Count == 0)
            {
                _requesters.Add(requester);
                _FadeOut.Kill();
                _FadeIn.Play();
            }
        }
        
        public async UniTask RequestOverlayAndWait(object requester, CancellationToken cancellationToken = default)
        {
            RequestOverlay(requester);
            var tween = _FadeIn.Tween;
            if (!tween.IsActive()) return;
            await tween;
        }

        public void ReleaseOverlay(object requester)
        {
            if (requester == null)
                return;

            if (_requesters.Remove(requester) && _requesters.Count == 0)
            {
                _FadeIn.Kill();
                _FadeOut.Play();
            }
        }
        
        [Button]
        private async UniTask Request()
        {
            Debug.Log("Request");
            await RequestOverlayAndWait(this);
            Debug.Log("Done");
        }
        
        [Button]
        private void Release()
        {
            ReleaseOverlay(this);
        }
    }
}
