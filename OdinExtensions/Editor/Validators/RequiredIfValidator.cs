using EDIVE.OdinExtensions.Attributes;
using EDIVE.OdinExtensions.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.OdinInspector.Editor.ValueResolvers;

[assembly: RegisterValidator(typeof(RequiredIfValidator<>))]
namespace EDIVE.OdinExtensions.Editor
{
    public class RequiredIfValidator<T> : AttributeValidator<RequiredIfAttribute, T>
        where T : class
    {
        private IfAttributeHelper _helper;
        private ValueResolver<string> _messageGetter;

        protected override void Initialize()
        {
            _helper = new IfAttributeHelper(Property, Attribute.Condition, true);
            if (Attribute.Message != null)
                _messageGetter = ValueResolver.GetForString(Property, Attribute.Message);
        }

        protected override void Validate(ValidationResult result)
        {
            if (_helper.GetValue(Attribute.Value) && !IsValid(ValueEntry.SmartValue))
            {
                var severity = Attribute.MessageType.ToValidatorSeverity();
                var message = _messageGetter != null ? _messageGetter.GetValue() : (Property.NiceName + " is required");
                result.Add(severity, message).WithFix<FixArgs<T>>(val =>
                {
                    Property.ValueEntry.WeakSmartValue = val.NewValue;
                });
            }
        }

        private static bool IsValid(T memberValue)
        {
            if (ReferenceEquals(memberValue, null))
                return false;
            if (memberValue is string value && string.IsNullOrEmpty(value))
                return false;
            if (memberValue is UnityEngine.Object obj && obj == null)
                return false;
            return true;
        }
    }

    [ShowOdinSerializedPropertiesInInspector]
    internal class FixArgs<T>
    {
        public T NewValue;
    }
}