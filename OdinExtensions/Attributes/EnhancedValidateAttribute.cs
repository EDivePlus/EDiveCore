using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public sealed class EnhancedValidateAttribute : Attribute
    {
        public string ValidationMethod;
        public bool IncludeChildren;
        public bool ContinuousValidationCheck;
        public bool ApplyToListElements;

        public EnhancedValidateAttribute(string validationMethod)
        {
            ValidationMethod = validationMethod;

            IncludeChildren = true;
        }
    }
}
