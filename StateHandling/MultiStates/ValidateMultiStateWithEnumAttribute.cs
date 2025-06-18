// Author: František Holubec
// Created: 18.06.2025

using System;
using System.Diagnostics;
using System.Linq;
using EDIVE.StateHandling.MultiStates;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
[assembly: RegisterValidator(typeof(ValidateMultiStateWithEnumAttributeValidator<>))]
#endif

namespace EDIVE.StateHandling.MultiStates
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class ValidateMultiStateWithEnumAttribute : Attribute
    {
        public Type EnumType;
        public Enum[] OptionalValues;

        public ValidateMultiStateWithEnumAttribute(Type enumType, params object[] optionalValues)
        {
            EnumType = enumType;
            OptionalValues = optionalValues.OfType<Enum>().ToArray();
        }
    }

#if UNITY_EDITOR
    public class ValidateMultiStateWithEnumAttributeValidator<T> : AttributeValidator<ValidateMultiStateWithEnumAttribute, T> where T : AMultiState
    {
        protected override void Validate(ValidationResult result)
        {
            if (Value == null) return;

            var allEnumValues = Enum.GetNames(Attribute.EnumType);
            var requiredEnumValues = allEnumValues.Except(Attribute.OptionalValues.Select(e => e.ToString()));

            var allStates = Value.GetAllStates();
            var missing = requiredEnumValues.Except(allStates);
            var extra = allStates.Except(allEnumValues);

            if (missing.Any() || extra.Any())
            {
                var message = "States mismatch";
                if (missing.Any()) message += $", missing: [{string.Join(", ", missing)}]";
                if (extra.Any()) message += $", extra: [{string.Join(", ", extra)}]";

                result.AddError(message)
                    .WithFix(() =>
                    {
                        foreach (var state in missing)
                        {
                            Value.AddState(state);
                        }

                        foreach (var state in extra)
                        {
                            Value.RemoveState(state);
                        }

                        if (!Value.HasState(Value.DefaultState))
                            Value.DefaultState = requiredEnumValues.FirstOrDefault();

                        Property.MarkSerializationRootDirty();
                    });
            }
        }
    }
#endif
}
