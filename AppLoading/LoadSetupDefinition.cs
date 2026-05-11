using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Dependencies;
using EDIVE.AppLoading.Finalizers;
using EDIVE.AppLoading.LoadItems;
using EDIVE.AppLoading.Utils;
using EDIVE.AssetTranslation;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.AppLoading
{
    public class LoadSetupDefinition : AUniqueDefinition
    {
        [EnhancedBoxGroup("Finalizer")]
        [HideLabel]
        [InlineProperty]
        [SerializeReference]
        private ILoadFinalizer _Finalizer;

        [ShowCreateNew]
        [EnhancedInlineEditor]
        [PropertySpace]
        [SerializeField]
        [EnhancedValidate("ValidateGroups")]
        internal List<LoadGroupDefinition> _Groups = new();

        public ILoadFinalizer Finalizer => _Finalizer;

        public IEnumerable<LoadGroupDefinition> Groups => _Groups;

        private DependencyResolutionContext _runtimeContext;
        public DependencyResolutionContext RuntimeContext => _runtimeContext;

        public void Initialize()
        {
            _runtimeContext = new DependencyResolutionContext(GetValidLoadItems());
            foreach (var group in _Groups)
            {
                group.Initialize(_runtimeContext);
            }
        }

        public void Terminate()
        {
            foreach (var group in _Groups)
            {
                group.Terminate();
            }
            _runtimeContext = null;
        }

        public async UniTask Load()
        {
            var disableParallelLoad = LoaderUtils.DisableParallelLoad;
            if (disableParallelLoad)
            {
                var sortedLoadItems = GetValidLoadItemsSorted();
                foreach (var loadItem in sortedLoadItems)
                {
                    await loadItem.Load();
                }
            }
            else
            {
                var groupTasks = _Groups
                    .Where(g => g.IsAvailable)
                    .Select(g => g.Load());
                await UniTask.WhenAll(groupTasks);
            }
        }

        public IEnumerable<LoadGroupDefinition> GetAvailableLoadGroups()
        {
            return _Groups.Where(group => group.IsAvailable);
        }

        public IEnumerable<LoadItemDefinition> GetValidLoadItems()
        {
            var availableGroups = GetAvailableLoadGroups();
            var groupIndex = -1;
            foreach (var availableGroup in availableGroups)
            {
                groupIndex++;
                if (availableGroup == null)
                {
                    Debug.LogError($"Group at index {groupIndex} is null ");
                    continue;
                }

                var itemIndex = -1;
                foreach (var loadItem in availableGroup.LoadItems)
                {
                    itemIndex++;
                    if (loadItem == null)
                    {
                        Debug.LogError($"Load item at index {itemIndex} in group '{availableGroup.UniqueID}' is null");
                        continue;
                    }

                    if (loadItem.CheckAvailability())
                        yield return loadItem;
                }
            }
        }

        public IEnumerable<LoadItemDefinition> GetValidLoadItemsSorted()
        {
            var ctx = _runtimeContext ?? new DependencyResolutionContext(GetValidLoadItems());
            return LoaderUtils.SortLoadItems(GetAvailableLoadGroups(), ctx).Where(l => l.CheckAvailability());
        }

#if UNITY_EDITOR
        [PropertySpace]
        [ShowInInspector]
        [PropertyOrder(50)]
        [Searchable]
        [ReadOnlyListElements]
        [CustomValueDrawer("DecoratedLoadItemDrawer")]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "OnAllLoadItemsTitleGUI")]
        public List<LoadItemDefinition> AllLoadItems { get; private set; }

        [ShowInInspector]
        [PropertyOrder(50)]
        [Searchable]
        [ReadOnlyListElements]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "OnLoadItemsWithoutGroupTitleGUI")]
        public List<LoadItemDefinition> LoadItemsWithoutGroup { get; private set; }

        [UsedImplicitly]
        private void OnAllLoadItemsTitleGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                ResolveAllLoadItems();
            }

            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.ArrowDownAZSolid))
            {
                AllLoadItems = LoaderUtils.SortLoadItems(_Groups, DependencyResolutionContext.EditorContext);
            }
        }

        [UsedImplicitly]
        private void OnLoadItemsWithoutGroupTitleGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                ResolveLoadItemsWithoutGroup();
            }
        }

        [OnInspectorInit]
        public void ResolveLoadItemsWithoutGroup()
        {
            LoadItemsWithoutGroup = EditorAssetUtils.FindAllAssetsOfType<LoadItemDefinition>()
                .Except(Groups.Where(g => g != null).SelectMany(g => g.LoadItems)).ToList();
        }

        [OnInspectorInit]
        public void ResolveAllLoadItems()
        {
            AllLoadItems = Groups.Where(g => g != null).SelectMany(g => g.LoadItems).Distinct().Where(i => i != null).ToList();
        }

        private LoadItemDefinition DecoratedLoadItemDrawer(LoadItemDefinition value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            return LoaderUtils.DecoratedLoadItemDrawer(value, label, callNextDrawer);
        }

        [UsedImplicitly]
        private void ValidateGroups(SelfValidationResult result)
        {
            var missingGroupTexts = new List<string>();
            foreach (var group in _Groups)
            {
                if (group == null) continue;
                group.ResolveCompleteDependencies();
                var missingGroups = group.TransitiveDependencies.Except(_Groups).ToList();
                if (missingGroups.Count > 0)
                    missingGroupTexts.Add($"[{group.name} - {string.Join(", ", missingGroups.Select(v => v.name))}]");
            }

            if (missingGroupTexts.Count > 0)
            {
                result.AddError($"Missing group dependencies: {string.Join(", ", missingGroupTexts)}");
            }

            ResolveAllLoadItems();
            var missingItemsTexts = new List<string>();
            foreach (var group in _Groups)
            {
                if (group == null) continue;
                foreach (var loadItem in group.LoadItems)
                {
                    if (loadItem == null) continue;
                    loadItem.ResolveTransitiveDependencies();
                    var missingItems = loadItem.TransitiveDependencies.Except(AllLoadItems).ToList();
                    if (missingItems.Count > 0)
                        missingItemsTexts.Add($"[{group.name}:{loadItem.name} - {string.Join(", ", missingItems.Select(v => v.name))}]");
                }
            }

            if (missingItemsTexts.Count > 0)
            {
                result.AddError($"Missing item dependencies: {string.Join(", ", missingItemsTexts)}");
            }

            var duplicateItems = _Groups.Where(g => g != null)
                .SelectMany(g => g.LoadItems.Select(i => new{Item = i, Group = g}))
                .GroupBy(i => i.Item)
                .Where(g => g.Count() > 1)
                .Select(i => $"[{i.Key.name} at {string.Join(",", i.Select(s => s.Group.name))}]")
                .ToList();

            if (duplicateItems.Count > 0)
            {
                result.AddError($"There are duplicate items: {string.Join(", ", duplicateItems)}");
            }

            // Warn when a setup contains >1 item representing the same type referenced by any TypedDependency.
            // At runtime the context would resolve the type to multiple candidates — ambiguous and likely wrong.
            var setupItems = _Groups.Where(g => g != null).SelectMany(g => g.LoadItems).Where(i => i != null).Distinct().ToList();
            // Build map: Type → items in this setup that represent it
            var typeToItems = new Dictionary<Type, List<LoadItemDefinition>>();
            foreach (var item in setupItems)
            {
                if (item.RepresentedTypes == null) continue;
                foreach (var ut in item.RepresentedTypes)
                {
                    var t = ut?.Value;
                    if (t == null) continue;
                    if (!typeToItems.TryGetValue(t, out var list))
                        typeToItems[t] = list = new List<LoadItemDefinition>();
                    list.Add(item);
                }
            }

            var ambiguousTexts = new List<string>();
            foreach (var item in setupItems)
            {
                foreach (var td in item.ResolvedDependencies)
                {
                    var t = td?.Type;
                    if (t == null) continue;
                    if (!typeToItems.TryGetValue(t, out var candidates) || candidates.Count <= 1) continue;
                    ambiguousTexts.Add($"{item.name} → {t.Name} ({string.Join(", ", candidates.Select(c => c.name))})");
                }
            }
            if (ambiguousTexts.Count > 0)
                result.AddError($"Ambiguous TypedDependencies (multiple items represent the same type): {string.Join("; ", ambiguousTexts)}");
        }
#endif
    }
}
