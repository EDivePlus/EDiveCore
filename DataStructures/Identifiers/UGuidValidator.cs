// Author: František Holubec
// Created: 08.06.2026

#if UNITY_EDITOR
using System;
using EDIVE.DataStructures.Identifiers;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidationRule(typeof(UGuidValidator))]
namespace EDIVE.DataStructures.Identifiers
{
    [Serializable]
    public class UGuidValidator : ValueValidator<UGuid>
    {
        [EnumToggleButtons]
        [SerializeField]
        private ValidatorSeverity _Severity = ValidatorSeverity.Warning;

        protected override void Validate(ValidationResult result)
        {
            if (Value.IsEmpty)
            {
                result
                    .Add(_Severity, "UnityID is empty. Are you sure this is correct?")
                    .WithFix(() => Value = UGuid.New());
            }
        }
    }
}

#endif