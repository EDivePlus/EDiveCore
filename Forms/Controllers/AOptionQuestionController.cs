// Author: Michal Petr
// Created: 31.10.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public abstract class AOptionQuestionController<TOption> : AFormQuestionController<AOptionQuestion<TOption>> 
        where TOption : IQuestionOption 
    {
        [SerializeField]
        private List<AOptionHandler<TOption>> _OptionHandlers = new();

        private readonly List<TOption> _selectedOptions = new();
        
        protected override void Initialize(AOptionQuestion<TOption> question)
        {
            _selectedOptions.Clear();
            
            var options = question.Options.Where(o => o != null).ToList();
            var optionHandlers = _OptionHandlers.Where(h => h != null).ToList();
            
            foreach (var handler in optionHandlers)
            {
                handler.SetVisible(false);
                handler.SetSelected(false, false);
            }
            
            if (options.Count == 0)
            {
                Debug.LogError("No options provided for the question.");
                return;
            }
            
            if (optionHandlers.Count < options.Count)
            {
                Debug.LogError("Not enough handlers for the options.");
                return;
            }

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var optionHandler = optionHandlers[i];
                optionHandler.SetVisible(true);
                optionHandler.Initialize(option);
                optionHandler.OptionSelectionChanged += OnOptionSelected;
            }
        }
        
        public override void Terminate()
        {
            foreach (var optionHandler in _OptionHandlers)
            {
                optionHandler.OptionSelectionChanged -= OnOptionSelected; 
                optionHandler.Terminate();
            }
        }

        private void OnOptionSelected(AOptionHandler<TOption> handler, bool selected)
        {
            var option = handler.Option;
            if (selected)
            {
                if (Question.SelectionLimits.y > 0 && _selectedOptions.Count >= Question.SelectionLimits.y)
                {
                    handler.SetSelected(false, false);
                    return;
                }

                if (!_selectedOptions.Contains(option))
                    _selectedOptions.Add(option);
            }
            else
            {
                _selectedOptions.Remove(option);
            }

            SetAnswer(new OptionFormAnswer<TOption>(_selectedOptions));
            RefreshState();
        }

        public void RefreshState()
        {
            var isAtLimit = Question.SelectionLimits.y > 1 && _selectedOptions.Count >= Question.SelectionLimits.y;
            foreach (var optionHandler in _OptionHandlers)
            {
                optionHandler.SetInteractable(!isAtLimit || _selectedOptions.Contains(optionHandler.Option));
            }
        }

        private void OnDestroy()
        {
            Terminate();
        }
    }
}