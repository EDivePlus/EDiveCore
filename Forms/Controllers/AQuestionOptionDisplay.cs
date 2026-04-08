// Author: František Holubec
// Created: 08.04.2026

using EDIVE.Forms.Questions;
using TMPro;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public abstract class AQuestionOptionDisplay : MonoBehaviour
    {
        public abstract IQuestionOption BaseOption { get; }

        public abstract void Initialize(IQuestionOption option);
        public abstract void Terminate();
    }

    public abstract class AQuestionOptionDisplay<TOption> : AQuestionOptionDisplay where TOption : IQuestionOption
    {
        [SerializeField]
        private TMP_Text _IdText;
        
        public TOption Option { get; private set; }
        public override IQuestionOption BaseOption => Option;

        public sealed override void Initialize(IQuestionOption option)
        {
            if (option is not TOption tOption)
            {
                Debug.LogError($"Invalid option type {option.GetType()}");
                return;
            }
            Option = tOption;
            if (_IdText)
                _IdText.text = Option.ID;
            Initialize(tOption);
        }

        protected abstract void Initialize(TOption option);
    }
}
