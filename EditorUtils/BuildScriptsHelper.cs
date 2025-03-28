#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;

namespace EDIVE.EditorUtils
{
    public static class BuildScriptsHelper
    {
        [MenuItem("Tools/Build Scripts Only", priority = 200)]
        public static void BuildScripts()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new string[]{},
                locationPathName = "ScriptBuilds",
                target = EditorUserBuildSettings.activeBuildTarget,
                options = BuildOptions.BuildScriptsOnly
            };
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }

        [MenuItem("Tools/Reload Domain", priority = 200)]
        public static void ReloadDomain()
        {
            EditorUtility.RequestScriptReload();
        }

        [MenuItem("Tools/Request Script Compilation", priority = 200)]
        public static void RecompileAllAssemblies()
        {
            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        }
    }
}
#endif

