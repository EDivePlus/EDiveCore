using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Finalizers;
using EDIVE.AppLoading.LoadItems;
using EDIVE.AppLoading.Utils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.UniqueDefinitions;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using Sirenix.OdinInspector.Editor.Validation;
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

        public void Initialize()
        {
            foreach (var group in _Groups)
            {
                group.Initialize();
            }
        }

        public void Terminate()
        {
            foreach (var group in _Groups)
            {
                group.Terminate();
            }
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

        public IEnumerable<ALoadItemDefinition> GetValidLoadItems()
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

                    if (loadItem.IsValid)
                        yield return loadItem;
                }
            }
        }

        public IEnumerable<ALoadItemDefinition> GetValidLoadItemsSorted()
        {
            return LoaderUtils.SortLoadItems(GetAvailableLoadGroups()).Where(l => l.IsValid);
        }

#if UNITY_EDITOR
        [PropertySpace]
        [ShowInInspector]
        [PropertyOrder(50)]
        [Searchable]
        [ReadOnlyListElements]
        [CustomValueDrawer("DecoratedLoadItemDrawer")]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "OnAllLoadItemsTitleGUI")]
        public List<ALoadItemDefinition> AllLoadItems { get; private set; }

        [ShowInInspector]
        [PropertyOrder(50)]
        [Searchable]
        [ReadOnlyListElements]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "OnLoadItemsWithoutGroupTitleGUI")]
        public List<ALoadItemDefinition> LoadItemsWithoutGroup { get; private set; }

        [UsedImplicitly]
        private void OnAllLoadItemsTitleGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                ResolveAllLoadItems();
            }

            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.ArrowDownAZSolid))
            {
                AllLoadItems = LoaderUtils.SortLoadItems(_Groups);
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
            LoadItemsWithoutGroup = EditorAssetUtils.FindAllAssetsOfType<ALoadItemDefinition>()
                .Except(Groups.Where(g => g != null).SelectMany(g => g.LoadItems)).ToList();
        }

        [OnInspectorInit]
        public void ResolveAllLoadItems()
        {
            AllLoadItems = Groups.Where(g => g != null).SelectMany(g => g.LoadItems).Distinct().Where(i => i != null).ToList();
        }

        private ALoadItemDefinition DecoratedLoadItemDrawer(ALoadItemDefinition value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            return LoaderUtils.DecoratedLoadItemDrawer(value, label, callNextDrawer);
        }

        [UsedImplicitly]
        private void ValidateGroups(ValidationResult result)
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
                    loadItem.ResolveCompleteDependencies();
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
        }
#endif
    }
}
