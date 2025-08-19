// Author: František Holubec
// Created: 18.06.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EDIVE.StateHandling.MultiStates;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.OdinInspector.Editor.ValueResolvers;
[assembly: RegisterValidator(typeof(ValidateMultiStateAttributeValidator<>))]
#endif

namespace EDIVE.StateHandling.MultiStates
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class ValidateMultiStateAttribute : Attribute
    {
        public IEnumerable<string> AllowedStates;
        public string AllowedStatesGetter;

        public ValidateMultiStateAttribute(Type enumType)
        {
            AllowedStates = Enum.GetNames(enumType);
        }
        
        public ValidateMultiStateAttribute(IEnumerable<string> allowedStates)
        {
            AllowedStates = allowedStates;
        }
        
        public ValidateMultiStateAttribute(params object[] allowedStates)
        {
            AllowedStates = allowedStates.Select(s => s.ToString());
        }
        
        public ValidateMultiStateAttribute(string allowedStatesGetter)
        {
            AllowedStatesGetter = allowedStatesGetter;
        }
    }

#if UNITY_EDITOR
    public class ValidateMultiStateAttributeValidator<T> : AttributeValidator<ValidateMultiStateAttribute, T> where T : AMultiState
    {
        private ValueResolver<IEnumerable> _allowedStatesResolver;

        protected override void Initialize()
        {
            if (Attribute.AllowedStatesGetter != null)
            {
                _allowedStatesResolver = ValueResolver.Get<IEnumerable>(Property, Attribute.AllowedStatesGetter);
            }
        }

        protected override void Validate(ValidationResult result)
        {
            if (_allowedStatesResolver != null && _allowedStatesResolver.HasError)
            { 
                result.AddError(_allowedStatesResolver.ErrorMessage);
                return;
            }
            
            if (Value == null)
                return;
            
            if (_allowedStatesResolver != null)
            {
                Attribute.AllowedStates = _allowedStatesResolver.GetValue().Cast<object>().Where(x => x != null).Select(x => x.ToString());
            }
            
            var allAllowedStates = Attribute.AllowedStates.Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();
            
            var allStates = Value.GetAllStates().ToList();
            var missing = allAllowedStates.Except(allStates).ToList();
            var extra = allStates.Except(allAllowedStates).ToList();

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
                            Value.DefaultState = allAllowedStates.FirstOrDefault();

                        Property.MarkSerializationRootDirty();
                    });
            }
        }
    }
#endif
}
