// Author: František Holubec
// Created: 06.05.2025

using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.DataStructures.ScriptableVariables
{
    public class ObjectVariableAssigner : MonoBehaviour
    {
        [SerializeField]
        [EnhancedTableList(ShowFoldout = false)]
        private List<ObjectVariableRecord> _Records;

        private void Awake()
        {
            AssignReferences();
        }

        private void AssignReferences()
        {
            if (_Records == null)
                return;

            foreach (var record in _Records)
            {
                record.Assign();
            }
        }

        [Serializable]
        public class ObjectVariableRecord
        {
            [Required]
            [ShowCreateNew]
            [SerializeField]
            private AScriptableVariable _Variable;

            [Required]
            [EnhancedObjectDrawer]
            [EnhancedValidate("ValidateValue")]
            [SerializeField]
            private Object _Value;

            public void Assign()
            {
                if (_Variable == null || _Value == null)
                    return;

                _Variable.TrySetObjectValue(_Value);
            }
#if UNITY_EDITOR
            private void ValidateValue(SelfValidationResult result)
            {
                if (_Variable == null)
                    return;

                if (!typeof(Object).IsAssignableFrom(_Variable.VariableType))
                {
                    result.AddError("Variable type is not of Unity Object type");
                    return;
                }

                if (_Value == null)
                    return;

                if (!_Variable.VariableType.IsAssignableFrom(_Value.GetType()))
                {
                    result.AddError($"Object '{_Value}' is not assignable to variable '{_Variable.VariableType}'");
                }
            }
#endif
        }
    }
}
