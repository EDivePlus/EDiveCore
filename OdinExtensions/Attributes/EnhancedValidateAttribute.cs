using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [DontApplyToListElements]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public sealed class EnhancedValidateAttribute : Attribute
    {
        public string ValidationMethod;
        public bool IncludeChildren;
        public bool ContinuousValidationCheck;

        public EnhancedValidateAttribute(string validationMethod)
        {
            ValidationMethod = validationMethod;

            IncludeChildren = true;
        }
    }
}
