// Author: František Holubec
// Created: 06.05.2025

using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.ScriptableArchitecture.Variables.Assigners
{
    public class ObjectVariableAssigner : AScriptableAssigner
    {
        [SerializeField]
        [EnhancedTableList(ShowFoldout = false)]
        private List<ObjectVariableRecord> _Records;

        protected override void AssignReferences()
        {
            if (_Records == null)
                return;

            foreach (var record in _Records)
            {
                record.Assign();
            }
        }

        protected override void UnassignReferences()
        {
            // OnDisable/Destroy order is not guaranteed, so we do not unassign references here.
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

            private void ValidateValue(SelfValidationResult result)
            {
                ScriptableArchitectureUtils.ValidateScriptableValue(result, _Variable, _Value);
            }
        }
    }
}
