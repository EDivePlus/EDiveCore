#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;

namespace EDIVE.EditorUtils
{
    public static class DefineUtility
    {
        public static void SetDefines(IEnumerable<string> defines, bool enabled, IEnumerable<BuildTargetGroup> groups = null)
        {
            if (enabled) AddDefines(defines, groups);
            else RemoveDefines(defines, groups);
        }

        public static void AddDefines(IEnumerable<string> defines, IEnumerable<BuildTargetGroup> groups = null)
        {
            groups ??= GetValidBuildTargetGroups();
            var definesList = defines.ToList();
            foreach (var group in groups)
            {
                GetScriptingDefineSymbolsForGroup(group, out var currentDefines);

                foreach (var define in definesList)
                {
                    if (currentDefines.Contains(define))
                        continue;

                    currentDefines.Add(define);
                }
                SetScriptingDefineSymbolsForGroup(group, currentDefines);
            }
        }

        public static void RemoveDefines(IEnumerable<string> defines, IEnumerable<BuildTargetGroup> groups = null)
        {
            groups ??= GetValidBuildTargetGroups();
            var definesList = defines.ToList();
            foreach (var group in groups)
            {
                GetScriptingDefineSymbolsForGroup(group, out var currentDefines);

                foreach (var define in definesList)
                {
                    if (!currentDefines.Contains(define))
                        continue;

                    currentDefines.Remove(define);
                }
                SetScriptingDefineSymbolsForGroup(group, currentDefines);
            }
        }

        public static void SetDefine(string define, bool enabled, IEnumerable<BuildTargetGroup> groups = null)
        {
            if (enabled) AddDefine(define, groups);
            else RemoveDefine(define, groups);
        }

        public static bool AddDefine(string define, IEnumerable<BuildTargetGroup> groups = null)
        {
            groups ??= GetValidBuildTargetGroups();
            var added = false;

            foreach (var group in groups)
            {
                GetScriptingDefineSymbolsForGroup(group, out var currentDefines);

                if (currentDefines.Contains(define))
                    continue;

                currentDefines.Add(define);
                SetScriptingDefineSymbolsForGroup(group, currentDefines);
                added = true;
            }

            return added;
        }

        public static bool RemoveDefine(string define, IEnumerable<BuildTargetGroup> groups = null)
        {
            groups ??= GetValidBuildTargetGroups();
            var removed = false;

            foreach (var group in groups)
            {
                GetScriptingDefineSymbolsForGroup(group, out var currentDefines);

                if (!currentDefines.Contains(define))
                    continue;

                currentDefines.Remove(define);
                SetScriptingDefineSymbolsForGroup(group, currentDefines);
                removed = true;
            }

            return removed;
        }

        public static void GetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup, out List<string> defines)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out var definesArray);
            defines = definesArray.ToList();
        }

        public static void SetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup, List<string> defines)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines.ToArray());
        }

        private static IEnumerable<BuildTargetGroup> GetValidBuildTargetGroups()
        {
            return Enum.GetValues(typeof(BuildTargetGroup))
                .Cast<BuildTargetGroup>()
                .Where(group => group != BuildTargetGroup.Unknown &&
                                typeof(BuildTargetGroup).GetField(group.ToString()).GetCustomAttribute<ObsoleteAttribute>() == null);
        }
    }
}
#endif
