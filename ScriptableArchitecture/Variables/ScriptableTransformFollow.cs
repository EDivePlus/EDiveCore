// Author: František Holubec
// Created: 12.06.2025

using EDIVE.NativeUtils;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Variables
{
    public class ScriptableTransformFollow : MonoBehaviour
    {
        [SerializeField]
        private AScriptableVariable<Transform> _Source;

        public AScriptableVariable<Transform> Source
        {
            get => _Source;
            set => _Source = value;
        }
        
        private void LateUpdate()
        {
            if (_Source?.Value)
            {
                transform.position = _Source.Value.position;
                transform.rotation = _Source.Value.rotation;
            }
        }
      
        // This does not work for some reason, i guess networking, or execution order...
        /*
        private Transform _currentTarget;

        private void OnEnable()
        {
            if (_Source != null)
            {
                _Source.ValueChanged.AddListener(OnSourceValueChanged);
                OnSourceValueChanged();
            }
        }

        private void OnDisable()
        {
            if (_Source != null) 
                _Source.ValueChanged.RemoveListener(OnSourceValueChanged);
        }

        private void OnSourceValueChanged()
        {
            if (_currentTarget != null) _currentTarget.RemoveChangeListener(OnTransformChanged);
            _currentTarget = _Source.Value;
            if (_currentTarget != null)
            {
                _currentTarget.AddChangeListener(OnTransformChanged);
                OnTransformChanged(_currentTarget);
            }
        }

        private void OnTransformChanged(Transform tr)
        {
            transform.position = _currentTarget.position;
            transform.rotation = _currentTarget.rotation;
        }
        */
    }
}
