// Author: František Holubec
// Created: 16.04.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public abstract class AOptionHandlerBundle : MonoBehaviour 
    {
        public abstract event Action<IQuestionOption, bool> SelectionChanged;

        public abstract void Initialize(AOptionsQuestion question);
        public abstract void Terminate();
        public abstract IEnumerable<IFormAnswerMetadata> CollectMetadata();
        public abstract void SetSelected(IQuestionOption option, bool selected, bool notify = true);
        public abstract void SetInteractable(IQuestionOption option, bool interactable);
    }
    
    public abstract class AOptionHandlerBundle<TOptionHandler> : AOptionHandlerBundle  where TOptionHandler : AOptionHandler
    {
        [SerializeField]
        private List<TOptionHandler> _OptionHandlers = new();
        
        public override event Action<IQuestionOption, bool> SelectionChanged;
        
        public override void Initialize(AOptionsQuestion question)
        {
            var options = question.BaseOptions.Where(o => o != null).ToList();
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
                optionHandler.SelectionChanged += OnOptionSelected;
            }
        }
        
        public override void Terminate()
        {
            foreach (var optionHandler in _OptionHandlers)
            {
                optionHandler.SelectionChanged -= OnOptionSelected; 
                optionHandler.Terminate();
            }
        }

        public override IEnumerable<IFormAnswerMetadata> CollectMetadata()
        {
            return _OptionHandlers.SelectMany(h => h.CollectMetadata());
        }

        public override void SetSelected(IQuestionOption option, bool selected, bool notify = true)
        {
            if (TryGetHandler(option, out var handler))
                handler.SetSelected(selected, notify);
        }
                     
        public override void SetInteractable(IQuestionOption option, bool interactable)
        {
            if (TryGetHandler(option, out var handler))
                handler.SetInteractable(interactable);
        }
        
        protected bool TryGetHandler(IQuestionOption option, out TOptionHandler handler)
        {
            return _OptionHandlers.TryGetFirst(h => h != null && h.Option == option, out handler);
        }
        
        private void OnOptionSelected(AOptionHandler handler, bool selected)
        {
            SelectionChanged?.Invoke(handler.Option, selected);
        }
    }
}
