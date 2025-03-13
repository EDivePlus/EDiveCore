using System;
using System.Diagnostics;
using UnityEngine;

namespace EDIVE.OdinExtensions.Attributes
{

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public class ReadOnlyListElementsAttribute : PropertyAttribute
    {
        public ReadOnlyListElementsAttribute() { }
    }
}
