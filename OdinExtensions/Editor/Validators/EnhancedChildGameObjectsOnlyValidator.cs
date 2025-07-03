// Author: František Holubec
// Created: 03.07.2025

using EDIVE.OdinExtensions.Attributes;
using EDIVE.OdinExtensions.Editor.Validators;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidator(typeof(EnhancedChildGameObjectsOnlyValidator<>))]
namespace EDIVE.OdinExtensions.Editor.Validators
{
    public class EnhancedChildGameObjectsOnlyValidator<T> : AttributeValidator<EnhancedChildGameObjectsOnlyAttribute, T> where T : Object
    {
        protected override void Validate(ValidationResult result)
        {
            var ownerGo = result.Setup.Root as GameObject;
            if (ownerGo == null)
            {
                var component = result.Setup.Root as Component;
                if (component != null)
                    ownerGo = component.gameObject;
            }

            var valueGo = ValueEntry.SmartValue as GameObject;
            if (valueGo == null)
            {
                var component = ValueEntry.SmartValue as Component;

                if (component != null)
                {
                    valueGo = component.gameObject;
                }
            }

            // Attribute doesn't apply in this context, as we're not on a GameObject
            // or are not dealing with the right kind of value
            if (ownerGo == null || valueGo == null)
                return;

            if (Attribute.IncludeSelf && ownerGo == valueGo)
                return;

            var current = valueGo.transform;
            while (true)
            {
                current = current.parent;
                if (current == null)
                    break;

                if (current.gameObject == ownerGo)
                    return;
            }

            result.AddError($"{valueGo.name} must be a child of {ownerGo.name}");
        }
    }
}
