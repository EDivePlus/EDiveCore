using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.AppLoading.Dependencies;
using EDIVE.AppLoading.LoadItems;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.AppLoading.Utils
{
    public static class LoaderUtils
    {
        private const string DISABLE_PARALLEL_LOAD_PREFS_KEY = "AppLoader.DisableParallelLoad";
        public static bool DisableParallelLoad { get => PlayerPrefs.GetInt(DISABLE_PARALLEL_LOAD_PREFS_KEY, 0) != 0; set => PlayerPrefs.SetInt(DISABLE_PARALLEL_LOAD_PREFS_KEY, value ? 1 : 0); }

        public static List<T> GetLoadableComponents<T>(this GameObject go)
        {
            var components = new List<T>();
            if (go != null)
            {
                components.AddRange(go.GetComponents<T>());
                if (go.TryGetComponent<PrefabChildLoadableProvider>(out var provider))
                {
                    components.AddRange(provider.GetLoadableComponents<T>());
                }
            }

            return components;
        }

        public static List<LoadItemDefinition> SortLoadItems(IEnumerable<LoadGroupDefinition> groups, DependencyResolutionContext context)
        {
            var sortedList = new List<LoadItemDefinition>();
            var visited = new HashSet<LoadItemDefinition>();

            var groupList = groups as IList<LoadGroupDefinition> ?? groups.ToList();
            foreach (var group in groupList)
            {
                group.PrepareSorting();
            }

            var nodes = groupList.SelectMany(g => g.LoadItems);
            foreach (var loadItem in nodes)
            {
                if (!visited.Contains(loadItem))
                {
                    TopologicalSortUtil(loadItem, visited, sortedList, context);
                }
            }

            return sortedList;
        }

        private static void TopologicalSortUtil(LoadItemDefinition loadItem, HashSet<LoadItemDefinition> visited, List<LoadItemDefinition> sortedList, DependencyResolutionContext context)
        {
            if (loadItem == null)
                return;

            if (!visited.Add(loadItem))
                return;

            foreach (var dependency in loadItem.GetSortingDependencies(context))
            {
                if (!visited.Contains(dependency))
                {
                    TopologicalSortUtil(dependency, visited, sortedList, context);
                }
            }

            // Directly add the node to the sorted list instead of pushing it onto a stack
            sortedList.Add(loadItem);
        }

#if UNITY_EDITOR
        public static IEnumerable<LoadItemDefinition> FindLoadItems(List<Type> requiredTypes)
        {
            var allLoadItems = EditorAssetUtils.FindAllAssetsOfType<LoadItemDefinition>();
            foreach (var loadItem in allLoadItems)
            {
                var representedTypes = loadItem.GetRepresentedTypes().ToList();
                foreach (var type in requiredTypes)
                {
                    if (!representedTypes.Any(t => type.IsAssignableFrom(t)))
                        continue;

                    yield return loadItem;
                    break;
                }
            }
        }

        public static LoadItemDefinition DecoratedLoadItemDrawer(LoadItemDefinition value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            GUILayout.BeginHorizontal();
            if (Application.isPlaying)
            {
                GUIHelper.PushContentColor(value.CurrentState.GetColor());
                GUILayout.Label(GUIHelper.TempContent(value.CurrentState.GetEditorIcon().Highlighted), GUILayout.Width(20), GUILayout.Height(18));
                GUIHelper.PopContentColor();
            }

            callNextDrawer(label);
            GUILayout.EndHorizontal();
            return value;
        }
#endif
    }
}
