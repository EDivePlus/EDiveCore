using System;
using System.Diagnostics;
using UnityEngine;

namespace EDIVE.OdinExtensions.Attributes
{
    /// <summary>
    /// Apply this attribute to ScriptableObject fields to draw a 'New' button for creating a new instance of the given ScriptableObject class or its subtypes.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    [Conditional("UNITY_EDITOR")]
    public class ShowCreateNewAttribute : PropertyAttribute
    {
        public Type OverrideType;
        
        public string GetOverrideTypeFunc;

        public string DefaultPath;
        
        public string ShowIf;
        
        public string OnCreatedNew;

        public string OverrideDefaultName;

        public ShowCreateNewAttribute() { }

        public ShowCreateNewAttribute(string defaultPath, string getOverrideTypeFunc = null)
        {
            DefaultPath = defaultPath;
            GetOverrideTypeFunc = getOverrideTypeFunc;
        }
    }
}