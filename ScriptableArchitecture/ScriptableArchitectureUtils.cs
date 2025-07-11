// Author: František Holubec
// Created: 07.05.2025

using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using EDIVE.External.DomainReloadHelper;
#endif

namespace EDIVE.ScriptableArchitecture
{
    public static class ScriptableArchitectureUtils
    {
#if UNITY_EDITOR
        [ExecuteOnReload(-1000)]
        private static void OnReload()
        {
            var scriptables = EditorAssetUtils.FindAllAssetsOfType<AScriptableBase>();
            foreach (var scriptable in scriptables)
            {
                scriptable.ResetState();
            }
        }
#endif

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
