// Author: František Holubec
// Created: 04.05.2026

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    [Serializable]
    public class QuestionTiming
    {
        [SerializeField]
        private bool _EnableTiming;
        
        [ShowIf(nameof(_EnableTiming))]
        [SerializeField]
        [MinValue(0f)]
        private float _PreparingDuration = 2f;
        
        [ShowIf(nameof(_EnableTiming))]
        [SerializeField]
        [MinValue(0f)]
        private float _ReadingDuration = 5f;
        
        [ShowIf(nameof(_EnableTiming))]
        [SerializeField]
        [MinValue(0f)]
        private float _AnsweringDuration = 20f;

        [ShowIf(nameof(_EnableTiming))]
        [SerializeField]
        [MinValue(0f)]
        private float _SummaryDuration = 6f;

        public bool EnableTiming => _EnableTiming;
        public float PreparingDuration => _PreparingDuration;
        public float ReadDuration => _ReadingDuration;
        public float AnsweringDuration => _AnsweringDuration;
        public float SummaryDuration => _SummaryDuration;
        
        public float GetDurationForPhase(QuestionPhase phase)
        {
            return phase switch
            {
                QuestionPhase.Preparing => PreparingDuration,
                QuestionPhase.Reading => ReadDuration,
                QuestionPhase.Answering => AnsweringDuration,
                QuestionPhase.Summary => SummaryDuration,
                _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
            };
        }
    }
}
