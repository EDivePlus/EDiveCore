using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.AppLoading.LoadItems;

#if UNITY_EDITOR
using UnityEditor;
using EDIVE.EditorUtils;
#endif

namespace EDIVE.AppLoading.Dependencies
{
    public class DependencyResolutionContext
    {
#if UNITY_EDITOR
        private static DependencyResolutionContext _editorContext;
        public static DependencyResolutionContext EditorContext => _editorContext ??= new DependencyResolutionContext(EditorAssetUtils.FindAllAssetsOfType<LoadItemDefinition>());

        private class LoadItemPostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                _editorContext = null;
            }
        }
#endif
        
        private readonly List<LoadItemDefinition> _allItems;
        private readonly Dictionary<Type, List<LoadItemDefinition>> _cache = new();

        public IEnumerable<LoadItemDefinition> AllItems => _allItems;

        public DependencyResolutionContext(IEnumerable<LoadItemDefinition> items)
        {
            _allItems = items
                .Where(i => i != null)
                .Distinct()
                .ToList();
        }

        public IEnumerable<LoadItemDefinition> GetItemsRepresentingType(Type type)
        {
            if (type == null) return Array.Empty<LoadItemDefinition>();
            if (_cache.TryGetValue(type, out var cached)) return cached;

            var matches = new List<LoadItemDefinition>();
            foreach (var item in _allItems)
            {
                if (item == null) continue;
                var represented = item.RepresentedTypes;
                if (represented == null) continue;
                foreach (var ut in represented)
                {
                    var represents = ut?.Value;
                    if (represents != null && type.IsAssignableFrom(represents))
                    {
                        matches.Add(item);
                        break;
                    }
                }
            }

            _cache[type] = matches;
            return matches;
        }
    }
}
