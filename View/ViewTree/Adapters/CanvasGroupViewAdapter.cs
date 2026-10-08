// Author: František Holubec
// Created: 08.10.2026

using UnityEngine;

namespace EDIVE.View.ViewTree
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupViewAdapter : AComponentViewAdapter<CanvasGroup>
    {
        private float _alpha = 1f;
        private bool _interactable = true;
        private bool _blocksRaycasts = true;
        private bool _isCaptured;

        protected override void Awake()
        {
            CaptureAuthored();
            base.Awake();
        }

        protected override void ApplyState(ViewState state)
        {
            CaptureAuthored();
            if (Target == null)
                return;

            Target.alpha = state >= ViewState.Visible ? _alpha : 0f;
            Target.interactable = state >= ViewState.Focused && _interactable;
            Target.blocksRaycasts = state >= ViewState.Focused && _blocksRaycasts;
        }

        private void CaptureAuthored()
        {
            if (_isCaptured || Target == null)
                return;

            _alpha = Target.alpha;
            _interactable = Target.interactable;
            _blocksRaycasts = Target.blocksRaycasts;
            _isCaptured = true;
        }
    }
}
