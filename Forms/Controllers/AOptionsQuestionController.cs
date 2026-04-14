// Author: Michal Petr
// Created: 31.10.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public abstract class AOptionsQuestionController<TOptionQuestion>  : AFormQuestionController<TOptionQuestion> where TOptionQuestion : AOptionsQuestion
    {
        [SerializeField]
        private List<OptionHandlerBundle> _HandlerBundles = new();
        
        // Todo add dynamic/external OptionHandlerBundles - use Scriptable List maybe?
        private IEnumerable<OptionHandlerBundle> FilteredHandlerBundles => _HandlerBundles.Where(b => b != null);
        
        private readonly List<IQuestionOption> _selectedOptions = new();

        protected override void Initialize(TOptionQuestion question)
        {
            _selectedOptions.Clear();

            foreach (var handlerBundle in FilteredHandlerBundles)
            {
                handlerBundle.Initialize(question);
                handlerBundle.SelectionChanged += OnSelectionChanged;
            }
            RefreshState();
        }
        
        public override void Terminate()
        {
            foreach (var handlerBundle in FilteredHandlerBundles)
            {
                handlerBundle.Terminate();
                handlerBundle.SelectionChanged += OnSelectionChanged;
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
            SetAnswer(CreateAnswer(_selectedOptions));
            RefreshState();
        }

        protected virtual AFormAnswer CreateAnswer(List<IQuestionOption> selectedOptions)
        {
            return new OptionFormAnswer(selectedOptions.Select(o => o.ID), CollectMetadata());
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
        
        private void OnDestroy()
        {
            Terminate();
        }
    }
}