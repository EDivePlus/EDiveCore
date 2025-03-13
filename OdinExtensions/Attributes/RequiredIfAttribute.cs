using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public class RequiredIfAttribute : Attribute
    {
        public string Message;
        public InfoMessageType MessageType;
        public string Condition;
        public object Value;

        public RequiredIfAttribute(string condition)
        {
            Condition = condition;
            MessageType = InfoMessageType.Error;
        }

        public RequiredIfAttribute(string condition, object optionalValue)
        {
            Condition = condition;
            Value = optionalValue;
            MessageType = InfoMessageType.Error;
        }
    }
}
