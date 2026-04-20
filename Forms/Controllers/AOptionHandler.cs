// Author: František Holubec
// Created: 07.04.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public abstract class AOptionHandler : MonoBehaviour
    {
        [SerializeField]
        private AToggleState _VisibleState;
        
        [SerializeField]
        private AToggleState _InteractableState;
        
        [Required]
        [SerializeField]
        private AMultiState _ResultState;
        
        [SerializeField]
        private AQuestionOptionDisplay _OptionDisplay;
        
        public IQuestionOption Option { get; private set; }

        public event Action<AOptionHandler, bool> SelectionChanged;

        public void Initialize(IQuestionOption option)
        {
            Option = option;
            InitializeInternal();
            if (_OptionDisplay)
                _OptionDisplay.Initialize(option);
            SetSelected(false, false);
        }
        
        public virtual void Terminate()
        {
            TerminateInternal();
            if (_OptionDisplay)
                _OptionDisplay.Terminate();
            Option = null;
        }
        
        public void SetVisible(bool visible)
        {
            if (_VisibleState)
                _VisibleState.SetState(visible);
            else
                gameObject.SetActive(visible);
        }
                
        public virtual void SetInteractable(bool interactable)
        {
            if (_InteractableState)
                _InteractableState.SetState(interactable);
        }
        
        
        public virtual IEnumerable<IFormAnswerMetadata> CollectMetadata() => Enumerable.Empty<IFormAnswerMetadata>();

        public abstract void SetSelected(bool selected, bool notify = true);
        public abstract void InitializeInternal();
        public abstract void TerminateInternal();

        protected void InvokeSelectionChanged(bool state)
        {
            var result = ResolveResultType(state);
            if (_ResultState)
                _ResultState.SetState(result);
            SelectionChanged?.Invoke(this, state);
        }

        private OptionResultType ResolveResultType(bool selected)
        {
            if (Option == null || !selected)
                return OptionResultType.None;

            return Option.IsCorrect ? OptionResultType.Correct : OptionResultType.Incorrect;
        }
    }
}
