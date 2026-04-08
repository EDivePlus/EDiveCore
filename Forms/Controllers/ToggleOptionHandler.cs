// Author: František Holubec
// Created: 08.04.2026

using System;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Forms.Controllers
{
    public class BubblePopOptionHandler : AOptionHandler
    {
        [SerializeField]
        private Toggle _Toggle;
        
        private Action<IOptionSelector, bool> _callback;

        public override void InitializeInternal()
        {
            if (_Toggle != null)
                _Toggle.onValueChanged.AddListener(InvokeSelectionChanged);
        }

        public override void TerminateInternal()
        {
            if (_Toggle != null)
                _Toggle.onValueChanged.RemoveListener(InvokeSelectionChanged);
        }
        
        public override void SetSelected(bool selected, bool notify = true)
        {
            if (_Toggle)
            {
                if (notify)
                    _Toggle.isOn = selected;
                else
                    _Toggle.SetIsOnWithoutNotify(selected);
            }
        }
    }
    
    public class ToggleOptionHandler : AOptionHandler
    {
        [SerializeField]
        private Toggle _Toggle;
        
        private Action<IOptionSelector, bool> _callback;

        public override void InitializeInternal()
        {
            if (_Toggle != null)
                _Toggle.onValueChanged.AddListener(InvokeSelectionChanged);
        }

        public override void TerminateInternal()
        {
            if (_Toggle != null)
                _Toggle.onValueChanged.RemoveListener(InvokeSelectionChanged);
        }
        
        public override void SetSelected(bool selected, bool notify = true)
        {
            if (_Toggle)
            {
                if (notify)
                    _Toggle.isOn = selected;
                else
                    _Toggle.SetIsOnWithoutNotify(selected);
            }
        }
    }
}
