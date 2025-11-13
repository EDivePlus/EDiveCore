// Author: František Holubec
// Created: 13.11.2025

using System;
using System.Collections;
using EDIVE.DataStructures;
using EDIVE.DataStructures.VariableFields;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.ScriptableArchitecture
{
    [Serializable]
    public class ScriptableVariableAssignAction : IAction
    {
        [HideLabel]
        [InlineProperty]
        [SerializeReference]
        [EnhancedValueDropdown("GetAssignmentDataDropdown")]
        private AssignmentData _Data;

        public void Execute() => _Data?.Apply();

        [Serializable]
        private abstract class AssignmentData
        {
            public abstract void Apply();
        }

        [Serializable]
        private class AssignmentData<T> : AssignmentData
        {
            [EnhancedAssetSelector]
            [SerializeField]
            private AScriptableVariable<T> _Variable;

            [SerializeField]
            private VariableField<T> _AssignedValue;

            public override void Apply()
            {
                if (_Variable != null && _AssignedValue != null)
                    _Variable.Value = _AssignedValue.Value;
            }

            public override string ToString() => typeof(T).Name;
        }

#if UNITY_EDITOR
        private IEnumerable GetAssignmentDataDropdown()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(AScriptableVariable<>)))
            {
                if (type.IsAbstract)
                    continue;
                
                var baseType = type.BaseType;
                while (baseType != null && (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(AScriptableVariable<>)))
                    baseType = baseType.BaseType;

                if (baseType == null)
                    continue;

                var tArg = baseType.GetGenericArguments()[0];
                var assignmentDataType = typeof(AssignmentData<>).MakeGenericType(tArg);
                var instance = Activator.CreateInstance(assignmentDataType);

                yield return new ValueDropdownItem(tArg.Name, instance);
            }
        }
#endif
    }
    
}
