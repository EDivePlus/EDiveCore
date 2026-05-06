// Author: Michal Petr
// Created: 31.10.2025

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public abstract class AOptionsQuestionController<TOptionQuestion> : AFormQuestionController<TOptionQuestion> where TOptionQuestion : AOptionsQuestion
    {
        [SerializeField]
        private List<AOptionHandlerBundle> _HandlerBundles = new();

        // Todo add dynamic/external OptionHandlerBundles - use Scriptable List maybe?
        public IEnumerable<AOptionHandlerBundle> FilteredHandlerBundles => _HandlerBundles.Where(b => b != null);
        
        public event Action CorrectAnswerSelected;

        private readonly List<IQuestionOption> _selectedOptions = new();

        protected override void Initialize()
        {
            base.Initialize();
            _selectedOptions.Clear();

            foreach (var handlerBundle in FilteredHandlerBundles)
            {
                handlerBundle.Initialize(Question);
                handlerBundle.SelectionChanged += OnSelectionChanged;
            }
            RefreshState();
        }

        public override void Terminate()
        {
            base.Terminate();

            foreach (var handlerBundle in FilteredHandlerBundles)
            {
                handlerBundle.Terminate();
                handlerBundle.SelectionChanged -= OnSelectionChanged;
            }
        }

        protected override void SetPhase(QuestionPhase phase, float duration = 0)
        {
            base.SetPhase(phase, duration);
            foreach (var handlerBundle in FilteredHandlerBundles)
            {
                handlerBundle.OnPhaseChanged(phase);
            }
        }

        private void OnSelectionChanged(IQuestionOption option, bool selected)
        {
            if (selected)
            {
                if (!_selectedOptions.Contains(option))
                    _selectedOptions.Add(option);
            }
            else
            {
                _selectedOptions.Remove(option);
            }

            SetSelected(option, selected, false);
            SubmitAnswer(CreateAnswer(_selectedOptions));
            RefreshState();
        }

        protected virtual AFormAnswer CreateAnswer(List<IQuestionOption> selectedOptions)
        {
            if (selectedOptions.Any(o => o.IsCorrect))
                CorrectAnswerSelected?.Invoke();
            return new OptionFormAnswer(selectedOptions.Select(o => o.ID), CollectMetadata());
        }

        public override void SetAnswer(AFormAnswer answer)
        {
            if (answer is not OptionFormAnswer optionFormAnswer)
                return;

            var selectedOptions = Question.BaseOptions.Where(o => optionFormAnswer.OptionIDs.Contains(o.ID)).ToList();
            _selectedOptions.Clear();
            _selectedOptions.AddRange(selectedOptions);
            foreach (var option in Question.BaseOptions)
            {
                SetSelected(option, selectedOptions.Contains(option), false);
            }
            RefreshState();
        }

        protected virtual IEnumerable<IFormAnswerMetadata> CollectMetadata()
        {
            return _HandlerBundles.SelectMany(h => h.CollectMetadata());
        }

        public void SetSelected(IQuestionOption option, bool selected, bool notify = true)
        {
            FilteredHandlerBundles.ForEach(b => b.SetSelected(option, selected, notify));
        }

        public void SetInteractable(IQuestionOption option, bool interactable)
        {
            FilteredHandlerBundles.ForEach(b => b.SetInteractable(option, interactable));
        }

        public void RefreshState()
        {
            var isAtLimit = Question.SelectionLimits.y > 1 && _selectedOptions.Count >= Question.SelectionLimits.y;
            foreach (var option in Question.BaseOptions)
            {
                SetInteractable(option, !isAtLimit || _selectedOptions.Contains(option));
            }
        }

        protected override async UniTask PhaseTaskAsync(QuestionPhase phase, float duration, CancellationToken cancellationToken)
        {
            if (phase != QuestionPhase.Answering)
            {
                await base.PhaseTaskAsync(phase, duration, cancellationToken);
                return;
            }
            
            if (duration <= 0f)
                return;
            
            var correctAnswerTcs = new UniTaskCompletionSource();
            CorrectAnswerSelected += OnCorrectAnswer;
            await UniTask.WhenAny(correctAnswerTcs.Task, UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken));
            CorrectAnswerSelected -= OnCorrectAnswer;
            
            return;
            void OnCorrectAnswer() => correctAnswerTcs.TrySetResult();
        }
    }
}
