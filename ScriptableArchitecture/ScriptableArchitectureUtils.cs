// Author: František Holubec
// Created: 07.05.2025

using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace EDIVE.ScriptableArchitecture
{
    public static class ScriptableArchitectureUtils
    {
        public static void ValidateScriptableValue(SelfValidationResult result, AScriptableBase scriptable, object value)
        {
            if (scriptable == null)
                return;

            var targetGenericType = scriptable.GenericType;
            if (!typeof(Object).IsAssignableFrom(targetGenericType))
            {
                result.AddError("Target generic type is not UnityEngine.Object");
                return;
            }

            if (value == null)
                return;

            if (!targetGenericType.IsAssignableFrom(value.GetType()))
            {
                result.AddError($"Object '{value}' is not assignable to '{targetGenericType}'");
            }
        }
    }
}
