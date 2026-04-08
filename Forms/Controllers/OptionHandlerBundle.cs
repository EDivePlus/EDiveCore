// Author: František Holubec
// Created: 08.04.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.Forms.Questions;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public class OptionHandlerBundle : MonoBehaviour 
    {
        [SerializeField]
        private List<AOptionHandler> _OptionHandlers = new();
        
        public event Action<AOptionHandler, bool> SelectionChanged;
        
        public void Initialize(AOptionsQuestion question)
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
        
        public void Terminate()
        {
            foreach (var optionHandler in _OptionHandlers)
            {
                optionHandler.SelectionChanged -= OnOptionSelected; 
                optionHandler.Terminate();
            }
        }

        public void SetSelected(IQuestionOption option, bool selected, bool notify = true)
        {
            if (TryGetHandler(option, out var handler))
                handler.SetSelected(selected, notify);
        }
                     
        public void SetInteractable(IQuestionOption option, bool interactable)
        {
            if (TryGetHandler(option, out var handler))
                handler.SetInteractable(interactable);
        }
        
        private bool TryGetHandler(IQuestionOption option, out AOptionHandler handler)
        {
            return _OptionHandlers.TryGetFirst(h => h != null && h.Option == option, out handler);
        }
        
        private void OnOptionSelected(AOptionHandler handler, bool selected)
        {
            SelectionChanged?.Invoke(handler, selected);
        }
    }
}
