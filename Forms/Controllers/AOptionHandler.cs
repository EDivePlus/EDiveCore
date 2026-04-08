// Author: František Holubec
// Created: 07.04.2026

using System;
using EDIVE.Forms.Questions;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public abstract class AOptionHandler<TOption> : MonoBehaviour where TOption : IQuestionOption
    {
        [SerializeReference]
        private IOptionSelector _OptionSelector;
            
        [SerializeField]
        private AToggleState _VisibleState;
        
        public TOption Option { get; private set; }

        public event Action<AOptionHandler<TOption>, bool> OptionSelectionChanged;

        public void Initialize(TOption option)
        {
            Option = option;
            SetSelected(false, false);
            _OptionSelector?.RegisterSelectionListener(OnSelectorValueChanged);
        }
        
        public void Terminate()
        {
            Option = default;
            _OptionSelector?.UnregisterSelectionListener(OnSelectorValueChanged);
        }
        
        private void OnSelectorValueChanged(bool value)
        { 
            OptionSelectionChanged?.Invoke(this, value);
        }
        
        public void SetVisible(bool visible)
        {
            if (_VisibleState)
                _VisibleState.SetState(visible);
            else
                gameObject.SetActive(visible);
        }
        
        public void SetSelected(bool selected, bool notify = true) => _OptionSelector?.SetSelected(selected, notify);
        public void SetInteractable(bool interactable) => _OptionSelector?.SetInteractable(interactable);
    }
}
