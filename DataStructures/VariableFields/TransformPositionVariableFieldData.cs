// Author: František Holubec
// Created: 20.10.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures.VariableFields
{
    [Serializable]
    public class TransformPositionVariableFieldData : IVariableFieldData<Vector3>
    {
        [HideLabel]
        [SerializeField]
        private Transform _Target;
        
        public Vector3 Value
        {
            get => _Target != null ? _Target.position : default;
            set
            {
                if (_Target != null) 
                    _Target.position = value;
            }
        }
    }
}
