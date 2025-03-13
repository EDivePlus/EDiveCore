using EDIVE.OdinExtensions.Attributes;
using EDIVE.OdinExtensions.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.Validation;
using NamedValue = Sirenix.OdinInspector.Editor.ActionResolvers.NamedValue;

[assembly: RegisterValidator(typeof(EnhancedValidateAttributeValidator<>))]
namespace EDIVE.OdinExtensions.Editor
{
    public class EnhancedValidateAttributeValidator<T> : AttributeValidator<EnhancedValidateAttribute, T>
    {
        private static readonly NamedValue[] CUSTOM_VALIDATION_ARGS =
        {
            new("value", typeof(T)),
            new("result", typeof(ValidationResult))
        };

        private ActionResolver _validationChecker;

        public override RevalidationCriteria RevalidationCriteria
        {
            get
            {
                if (Attribute.ContinuousValidationCheck)
                    return RevalidationCriteria.Always;
                return Attribute.IncludeChildren ? RevalidationCriteria.OnValueChangeOrChildValueChange : RevalidationCriteria.OnValueChange;
            }
        }

        protected override void Initialize()
        {
            var context = ActionResolverContext.CreateDefault(Property, Attribute.ValidationMethod, CUSTOM_VALIDATION_ARGS);
            _validationChecker = ActionResolver.GetFromContext(ref context);
        }

        protected override void Validate(ValidationResult result)
        {
            if (_validationChecker.HasError)
            {
                result.Message = ActionResolver.GetCombinedErrors(_validationChecker);
                result.ResultType = ValidationResultType.Error;
            }
            else
            {
                _validationChecker.Context.NamedValues.Set("value", ValueEntry.SmartValue);
                _validationChecker.Context.NamedValues.Set("result", result);
                _validationChecker.DoAction();
            }
        }
    }
}
