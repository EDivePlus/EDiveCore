#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace EDIVE.EditorUtils
{
    public static class DefinesUtility
    {
        private static readonly NamedBuildTarget[] VALID_NAMED_BUILD_TARGETS =
        {
            NamedBuildTarget.Unknown,
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Server,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Android,
            NamedBuildTarget.WebGL,
            NamedBuildTarget.WindowsStoreApps,
            NamedBuildTarget.PS4,
            NamedBuildTarget.XboxOne,
            NamedBuildTarget.tvOS,
            NamedBuildTarget.VisionOS,
            NamedBuildTarget.NintendoSwitch,
            NamedBuildTarget.Stadia,
            NamedBuildTarget.LinuxHeadlessSimulation,
            NamedBuildTarget.EmbeddedLinux,
            NamedBuildTarget.QNX,
        };

        public static void SetDefines(IEnumerable<string> defines, bool enabled, IEnumerable<NamedBuildTarget> targets = null)
        {
            if (enabled) AddDefines(defines, targets);
            else RemoveDefines(defines, targets);
        }

        public static void AddDefines(IEnumerable<string> defines, IEnumerable<NamedBuildTarget> targets = null)
        {
            targets ??= VALID_NAMED_BUILD_TARGETS;
            var definesList = defines.ToList();
            foreach (var group in targets)
            {
                GetScriptingDefineSymbols(group, out var currentDefines);

                foreach (var define in definesList)
                {
                    if (currentDefines.Contains(define))
                        continue;

                    currentDefines.Add(define);
                }
                SetScriptingDefineSymbols(group, currentDefines);
            }
        }

        public static void RemoveDefines(IEnumerable<string> defines, IEnumerable<NamedBuildTarget> targets = null)
        {
            targets ??= VALID_NAMED_BUILD_TARGETS;
            var definesList = defines.ToList();
            foreach (var group in targets)
            {
                GetScriptingDefineSymbols(group, out var currentDefines);

                foreach (var define in definesList)
                {
                    if (!currentDefines.Contains(define))
                        continue;

                    currentDefines.Remove(define);
                }
                SetScriptingDefineSymbols(group, currentDefines);
            }
        }

        public static void SetDefine(string define, bool enabled, IEnumerable<NamedBuildTarget> targets = null)
        {
            if (enabled) AddDefine(define, targets);
            else RemoveDefine(define, targets);
        }

        public static bool AddDefine(string define, IEnumerable<NamedBuildTarget> targets = null)
        {
            targets ??= VALID_NAMED_BUILD_TARGETS;
            var added = false;

            foreach (var group in targets)
            {
                GetScriptingDefineSymbols(group, out var currentDefines);

                if (currentDefines.Contains(define))
                    continue;

                currentDefines.Add(define);
                SetScriptingDefineSymbols(group, currentDefines);
                added = true;
            }

            return added;
        }

        public static bool RemoveDefine(string define, IEnumerable<NamedBuildTarget> targets = null)
        {
            targets ??= VALID_NAMED_BUILD_TARGETS;
            var removed = false;

            foreach (var group in targets)
            {
                GetScriptingDefineSymbols(group, out var currentDefines);

                if (!currentDefines.Contains(define))
                    continue;

                currentDefines.Remove(define);
                SetScriptingDefineSymbols(group, currentDefines);
                removed = true;
            }

            return removed;
        }

        public static void GetScriptingDefineSymbols(NamedBuildTarget target, out List<string> defines)
        {
            PlayerSettings.GetScriptingDefineSymbols(target, out var definesArray);
            defines = definesArray.ToList();
        }

        public static void SetScriptingDefineSymbols(NamedBuildTarget target, List<string> defines)
        {
            PlayerSettings.SetScriptingDefineSymbols(target, defines.ToArray());
        }
    }
}
#endif
