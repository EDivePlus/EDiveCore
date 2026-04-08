// Author: Michal Petr
// Created: 29.10.2025

using System;
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
        
        protected VisualSwitcher Visual => _Visual;
        public abstract AFormQuestion BaseQuestion { get; }
        public AFormAnswer Answer { get; private set; }
        
        public event Action<AFormAnswer> AnswerChanged;
        
        public abstract bool IsSuitableFor(AFormQuestion question);
        public abstract void Initialize(AFormQuestion formQuestion);
        public abstract void Terminate();
        
        public void SetAnswer(AFormAnswer answer)
        {
            Answer = answer;
            AnswerChanged?.Invoke(answer);
        }
    }

    public abstract class AFormQuestionController<TQuestion> : AFormQuestionController 
        where TQuestion : AFormQuestion 
    {
        public TQuestion Question { get; private set; }
        public override AFormQuestion BaseQuestion => Question;
        
        protected abstract void Initialize(TQuestion question);
        public override void Initialize(AFormQuestion formQuestion)
        {
            if (formQuestion is not TQuestion tQuestion)
            {
                Debug.LogError($"Controller does not support Questions of type {typeof(TQuestion).Name}");
                return;
            }

            Question = tQuestion;
            Visual.Apply(tQuestion.Visual);
            Initialize(tQuestion);
        }
        
        public override bool IsSuitableFor(AFormQuestion question) => question is TQuestion tQuestion && IsSuitableFor(tQuestion);
        protected virtual bool IsSuitableFor(TQuestion question) => true;
    }
}
