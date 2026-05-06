// Author: Radim Holub
// Created: 29.04.2026

using System;
using DG.Tweening;
using EDIVE.StateHandling.MultiStates;
using EDIVE.Time.TimeSpanUtils;
using EDIVE.UIElements.ProgressBars;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public class QuestionPhaseDisplay : MonoBehaviour
    {
        [ValidateMultiState(typeof(QuestionPhase))]
        [SerializeField]
        private AMultiState _QuestionPhaseState;
        
        [SerializeField]
        private AProgressBar _ProgressBar;
   
        [SerializeField]
        private TimeSpanDisplay _TimeDisplay;

        [Required]
        [SerializeField]
        private AFormQuestionController _QuestionController;

        private Tween _progressTween;
        private float _timer;

        public void SetPhase(QuestionPhase phase, float duration)
        {
            if (_QuestionPhaseState)
                _QuestionPhaseState.SetState(phase);
            
            StartCountdown(duration);
        }
        
        private void OnDisable()
        {
            StopCountdown();
        }
        
        private void StartCountdown(float duration)
        {
            _progressTween?.Kill();
            if (duration <= 0f)
            {
                if (_ProgressBar != null)
                    _ProgressBar.Progress = 0f;

                if (_TimeDisplay != null)
                    _TimeDisplay.SetTimeSpan(TimeSpan.Zero);
                return;
            }
            
            _timer = duration;
            if (_ProgressBar != null)
                _ProgressBar.Progress = 1f;

            if (_TimeDisplay != null)
                _TimeDisplay.SetTimeSpan(TimeSpan.FromSeconds(_timer));

            _progressTween = DOTween.To(() => _timer, x =>
            {
                _timer = x;
                if (_ProgressBar != null)
                    _ProgressBar.Progress = x / duration;
                if (_TimeDisplay != null)
                    _TimeDisplay.SetTimeSpan(TimeSpan.FromSeconds(_timer));
            }, 0, duration)
                .SetEase(Ease.Linear);
        }
        
        private void StopCountdown()
        {
            _progressTween?.Kill();
            if (_ProgressBar != null)
                _ProgressBar.Progress = 0f;

            if (_TimeDisplay != null)
                _TimeDisplay.SetTimeSpan(TimeSpan.Zero);
        }
    }
}
