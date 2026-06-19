// Author: František Holubec

using EDIVE.EditorUtils;
using EDIVE.External.DomainReloadHelper;

namespace EDIVE.ScriptableArchitecture
{
    internal static class ScriptableArchitectureEditorHooks
    {
        [ExecuteOnReload(-1000)]
        private static void OnReload()
        {
            var scriptables = EditorAssetUtils.FindAllAssetsOfType<AScriptableBase>();
            foreach (var scriptable in scriptables)
            {
                scriptable.ResetState();
            }
        }
    }
}
