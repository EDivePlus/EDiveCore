// Author: František Holubec
// Created: 09.10.2026

using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EDIVE.Tweening;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;

namespace EDIVE.View.Toasts
{
    public class ToastDisplay : MonoBehaviour
    {
        [SerializeField]
        private VisualSwitcher _Switcher = new();

        [SerializeField]
        private TweenAnimationField _ShowAnimation;

        [SerializeField]
        private TweenAnimationField _HideAnimation;

        internal void Show(VisualPreset preset)
        {
            gameObject.SetActive(true);
            _Switcher.Apply(preset);

            // Shows from the hidden look
            _HideAnimation?.SetToEnd();
            _ShowAnimation?.Play();
        }

        internal async UniTask HideAsync(CancellationToken cancellationToken)
        {
            _ShowAnimation?.Kill();
            var tween = _HideAnimation?.Play();
            if (tween != null && tween.IsActive())
                await tween.ToUniTask(TweenCancelBehaviour.Kill, cancellationToken);

            gameObject.SetActive(false);
        }
    }
}
