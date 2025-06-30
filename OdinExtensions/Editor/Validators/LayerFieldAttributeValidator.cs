// Author: František Holubec
// Created: 30.06.2025

using System;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.OdinExtensions.Editor.Validators;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidator(typeof(LayerFieldAttributeValidator))]
namespace EDIVE.OdinExtensions.Editor.Validators
{
    public class LayerFieldAttributeValidator : AttributeValidator<LayerFieldAttribute, int>
    {
        protected override void Validate(ValidationResult result)
        {
            if (!string.IsNullOrEmpty(LayerMask.LayerToName(ValueEntry.SmartValue)))
                return;

            result.AddError("Invalid Layer")
                .WithFix(Fix.Create<LayerFix>(args => ValueEntry.SmartValue = args._Layer));
        }

        [Serializable]
        private class LayerFix
        {
            [LayerField]
            public int _Layer;
        }
    }
}
