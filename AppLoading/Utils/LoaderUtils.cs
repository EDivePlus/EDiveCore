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

        public static List<LoadItemDefinition> SortLoadItems(IEnumerable<LoadItemDefinition> loadItems, DependencyResolutionContext context)
        {
            return SortLoadItems(loadItems, i => i.GetSortingDependencies(context));
        }

        public static List<LoadItemDefinition> SortLoadItems(IEnumerable<LoadItemDefinition> loadItems, Func<LoadItemDefinition, IEnumerable<LoadItemDefinition>> getDependencies)
        {
            var sortedList = new List<LoadItemDefinition>();
            var visited = new HashSet<LoadItemDefinition>();

            foreach (var loadItem in loadItems)
            {
                if (!visited.Contains(loadItem))
                {
                    TopologicalSortUtil(loadItem, visited, sortedList, getDependencies);
                }
            }

            return sortedList;
        }

        private static void TopologicalSortUtil(LoadItemDefinition loadItem, HashSet<LoadItemDefinition> visited, List<LoadItemDefinition> sortedList,
            Func<LoadItemDefinition, IEnumerable<LoadItemDefinition>> getDependencies)
        {
            if (loadItem == null)
                return;

            if (!visited.Add(loadItem))
                return;

            foreach (var dependency in getDependencies(loadItem))
            {
                if (!visited.Contains(dependency))
                {
                    TopologicalSortUtil(dependency, visited, sortedList, getDependencies);
                }
            }

            // Directly add the node to the sorted list instead of pushing it onto a stack
            sortedList.Add(loadItem);
        }

        public static List<LoadItemDefinition> FindCycle(IEnumerable<LoadItemDefinition> loadItems, Func<LoadItemDefinition, IEnumerable<LoadItemDefinition>> getDependencies)
        {
            var done = new HashSet<LoadItemDefinition>();
            var path = new List<LoadItemDefinition>();
            foreach (var loadItem in loadItems)
            {
                var cycle = FindCycleUtil(loadItem, done, path, getDependencies);
                if (cycle != null)
                    return cycle;
            }

            return null;
        }

        private static List<LoadItemDefinition> FindCycleUtil(LoadItemDefinition loadItem, HashSet<LoadItemDefinition> done, List<LoadItemDefinition> path,
            Func<LoadItemDefinition, IEnumerable<LoadItemDefinition>> getDependencies)
        {
            if (loadItem == null || done.Contains(loadItem))
                return null;

            var index = path.IndexOf(loadItem);
            if (index >= 0)
                return path.Skip(index).Append(loadItem).ToList();

            path.Add(loadItem);
            foreach (var dependency in getDependencies(loadItem))
            {
                var cycle = FindCycleUtil(dependency, done, path, getDependencies);
                if (cycle != null)
                    return cycle;
            }

            path.RemoveAt(path.Count - 1);
            done.Add(loadItem);
            return null;
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
            if (Application.isPlaying && value != null)
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
