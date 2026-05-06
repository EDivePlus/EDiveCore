// Author: Michal Petr
// Created: 29.10.2025

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public abstract class AFormQuestionController : MonoBehaviour
    {
        [SerializeField]
        private VisualSwitcher _Visual;
        
        [SerializeField]
        private List<QuestionPhaseDisplay> _PhaseDisplays = new();

        protected VisualSwitcher Visual => _Visual;
        public abstract AFormQuestion BaseQuestion { get; }
        public AFormAnswer Answer { get; private set; }
        public QuestionPhase CurrentPhase { get; private set; }
        public FormController FormController { get; protected set; }

        public event Action<AFormAnswer> AnswerChanged;

        private CancellationTokenSource _phaseCancellation;

        public abstract bool IsSuitableFor(AFormQuestion question);
        public abstract void Initialize(FormController formController, AFormQuestion formQuestion);

        protected virtual void Initialize()
        {
            if (BaseQuestion.Timing.EnableTiming)
            {
                StartTimedPhases();
            }
            else
            {
                SetPhase(QuestionPhase.Answering);
            }
        }

        public virtual void Terminate()
        {
            StopTimedPhases();
        }

        protected void SubmitAnswer(AFormAnswer answer)
        {
            Answer = answer;
            AnswerChanged?.Invoke(answer);
        }

        protected virtual void SetPhase(QuestionPhase phase, float duration = 0f)
        {
            CurrentPhase = phase;
            foreach (var phaseDisplay in _PhaseDisplays)
            {
                if (phaseDisplay != null)
                    phaseDisplay.SetPhase(phase, duration);
            }
        }

        private void StartTimedPhases()
        {
            StopTimedPhases();
            _phaseCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            OrchestratePhases(_phaseCancellation.Token).Forget();
        }

        private void StopTimedPhases()
        {
            if (_phaseCancellation == null)
                return;
            _phaseCancellation.Cancel();
            _phaseCancellation.Dispose();
            _phaseCancellation = null;
        }

        private async UniTask OrchestratePhases(CancellationToken cancellationToken)
        {
            try
            {
                await RunPhase(QuestionPhase.Preparing, cancellationToken);
                await RunPhase(QuestionPhase.Reading, cancellationToken);
                await RunPhase(QuestionPhase.Answering, cancellationToken);
                await RunPhase(QuestionPhase.Summary, cancellationToken);
                
                if (FormController)
                    FormController.ShowNextQuestion();
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask RunPhase(QuestionPhase phase, CancellationToken cancellationToken)
        {
            var duration = BaseQuestion.Timing.GetDurationForPhase(phase);
            SetPhase(phase, duration);
            await PhaseTaskAsync(phase, duration, cancellationToken);
        }

        protected virtual async UniTask PhaseTaskAsync(QuestionPhase phase, float duration, CancellationToken cancellationToken)
        {
            if (duration > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);
        }

        public abstract void SetAnswer(AFormAnswer answer);

        private void OnDestroy()
        {
            Terminate();
        }
    }

    public abstract class AFormQuestionController<TQuestion> : AFormQuestionController
        where TQuestion : AFormQuestion
    {
        public TQuestion Question { get; private set; }
        public override AFormQuestion BaseQuestion => Question;

        public sealed override void Initialize(FormController formController, AFormQuestion formQuestion)
        {
            FormController = formController;
            if (formQuestion is not TQuestion tQuestion)
            {
                Debug.LogError($"Controller does not support Questions of type {typeof(TQuestion).Name}");
                return;
            }

            Question = tQuestion;
            Visual.Apply(tQuestion.Visual);
            Initialize();
        }

        public override bool IsSuitableFor(AFormQuestion question) => question is TQuestion tQuestion && IsSuitableFor(tQuestion);
        protected virtual bool IsSuitableFor(TQuestion question) => true;
    }
}
