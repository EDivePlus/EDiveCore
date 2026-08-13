// Author: František Holubec
// Created: 13.08.2026

using EDIVE.ScriptableArchitecture.Variables.Impl;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.StateHandling
{
    public class ToggleByScriptableVariable : MonoBehaviour
    {
        [SerializeField]
        private BoolScriptableVariable _Variable;
        
        [SerializeField]
        private AToggleState _Toggle;
        
        private void OnEnable()
        {
            if (_Variable != null)    
                _Variable.ValueChanged += OnVariableChanged;
            if (_Toggle) 
                _Toggle.State = _Variable.Value;
        }
        
        private void OnDisable()
        {
            if (_Variable != null)
                _Variable.ValueChanged -= OnVariableChanged;
        }

        private void OnVariableChanged()
        {
            if (_Toggle) 
                _Toggle.State = _Variable.Value;
        }
    }
}
