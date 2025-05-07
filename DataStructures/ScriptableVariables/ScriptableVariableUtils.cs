// Author: František Holubec
// Created: 07.05.2025

using EDIVE.EditorUtils;
using EDIVE.External.DomainReloadHelper;

namespace EDIVE.DataStructures.ScriptableVariables
{
    public static class ScriptableVariableUtils
    {
#if UNITY_EDITOR
        [ExecuteOnReload(-1000)]
        private static void OnReload()
        {
            var variables = EditorAssetUtils.FindAllAssetsOfType<AScriptableVariable>();
            foreach (var variable in variables)
            {
                variable.Clear();
            }
        }
#endif
    }
}
