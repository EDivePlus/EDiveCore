// Author: František Holubec
// Created: 07.04.2026

using System;
using EDIVE.Forms.Questions;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public abstract class AOptionHandler : MonoBehaviour
    {
        [SerializeField]
        private AToggleState _VisibleState;
        
        [SerializeField]
        private AToggleState _InteractableState;

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

        public abstract void SetSelected(bool selected, bool notify = true);
        public abstract void InitializeInternal();
        public abstract void TerminateInternal();
        
        protected void InvokeSelectionChanged(bool state)
        {
            SelectionChanged?.Invoke(this, state);
        }
    }
}
