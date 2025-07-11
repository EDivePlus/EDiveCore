// Author: František Holubec
// Created: 12.06.2025

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

        private void Update()
        {
            if (_Source?.Value)
            {
                transform.position = _Source.Value.position;
                transform.rotation = _Source.Value.rotation;
            }
        }
    }
}
