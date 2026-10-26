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
using UnityEditor;
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

        [PropertySpace]
        [Searchable]
        [SerializeField]
        [EnhancedValidate("ValidateLoadItems")]
        [CustomValueDrawer("DecoratedLoadItemDrawer")]
        [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = "OnLoadItemsTitleGUI")]
        private List<LoadItemDefinition> _LoadItems = new();

        public ILoadFinalizer Finalizer => _Finalizer;

        public IReadOnlyList<LoadItemDefinition> LoadItems => _LoadItems;

        private DependencyResolutionContext _runtimeContext;
        public DependencyResolutionContext RuntimeContext => _runtimeContext;

        public void Initialize()
        {
            _runtimeContext = new DependencyResolutionContext(GetValidLoadItems());
            foreach (var loadItem in _LoadItems)
            {
                if (loadItem == null) continue;
                if (!loadItem.IsValid)
                    Debug.LogError($"[{name}] Load item '{loadItem.name}' is not valid", loadItem);
                loadItem.Initialize(_runtimeContext);
            }

            var cycle = LoaderUtils.FindCycle(_runtimeContext.AllItems, i => i.GetSortingDependencies(_runtimeContext));
            if (cycle != null)
                Debug.LogError($"[{name}] Cyclic dependency, these items will never load: {string.Join(" → ", cycle.Select(i => i.name))}", this);
        }

        public void Terminate()
        {
            foreach (var loadItem in _LoadItems)
            {
                if (loadItem == null) continue;
                loadItem.Terminate();
            }
            _runtimeContext = null;
        }

        public async UniTask Load()
        {
            var disableParallelLoad = LoaderUtils.DisableParallelLoad;
            if (disableParallelLoad)
            {
                Debug.LogError("Parallel load is disabled. Loading items sequentially. This should only be used for testing purposes, as it can significantly increase load times.");
                var sortedLoadItems = GetValidLoadItemsSorted();
                foreach (var loadItem in sortedLoadItems)
                {
                    await loadItem.Load();
                }
            }
            else
            {
                await UniTask.WhenAll(_runtimeContext.AllItems.Select(l => l.Load()));
            }
        }

        public IEnumerable<LoadItemDefinition> GetValidLoadItems()
        {
            for (var i = 0; i < _LoadItems.Count; i++)
            {
                var loadItem = _LoadItems[i];
                if (loadItem == null)
                {
                    Debug.LogError($"[{name}] Load item at index {i} is null");
                    continue;
                }

                if (loadItem.CheckAvailability())
                    yield return loadItem;
            }
        }

        public IEnumerable<LoadItemDefinition> GetValidLoadItemsSorted()
        {
            var ctx = _runtimeContext ?? new DependencyResolutionContext(GetValidLoadItems());
            return LoaderUtils.SortLoadItems(ctx.AllItems, ctx).Where(l => l.CheckAvailability() && ctx.Contains(l));
        }

#if UNITY_EDITOR
        [PropertySpace]
        [ShowInInspector]
        [PropertyOrder(50)]
        [Searchable]
        [ReadOnlyListElements]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "OnLoadItemsNotInSetupTitleGUI")]
        public List<LoadItemDefinition> LoadItemsNotInSetup { get; private set; }

        [UsedImplicitly]
        private void OnLoadItemsTitleGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.ArrowDownAZSolid))
            {
                Undo.RecordObject(this, "Sort Load Items");
                var setupItems = _LoadItems.Where(i => i != null).Distinct().ToList();
                var setupContext = new DependencyResolutionContext(setupItems);
                var sorted = LoaderUtils.SortLoadItems(setupItems, i => i.EnumerateEditorPossibleDirectDependencies(setupContext));
                _LoadItems.RemoveAll(i => i != null);
                _LoadItems.AddRange(sorted.Where(setupContext.Contains));
                EditorUtility.SetDirty(this);
            }
        }

        [UsedImplicitly]
        private void OnLoadItemsNotInSetupTitleGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                ResolveLoadItemsNotInSetup();
            }
        }

        [OnInspectorInit]
        public void ResolveLoadItemsNotInSetup()
        {
            LoadItemsNotInSetup = EditorAssetUtils.FindAllAssetsOfType<LoadItemDefinition>().Except(_LoadItems).ToList();
        }

        private LoadItemDefinition DecoratedLoadItemDrawer(LoadItemDefinition value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            return LoaderUtils.DecoratedLoadItemDrawer(value, label, callNextDrawer);
        }

        [UsedImplicitly]
        private void ValidateLoadItems(SelfValidationResult result)
        {
            var setupItems = _LoadItems.Where(i => i != null).Distinct().ToList();
            var setupContext = new DependencyResolutionContext(setupItems);

            var missingItemsTexts = new List<string>();
            foreach (var loadItem in setupItems)
            {
                var missingItems = loadItem.EnumerateEditorPossibleDirectDependencies(DependencyResolutionContext.EditorContext, setupContext)
                    .Where(d => !setupContext.Contains(d))
                    .ToList();
                if (missingItems.Count > 0)
                    missingItemsTexts.Add($"[{loadItem.name} - {string.Join(", ", missingItems.Select(v => v.name))}]");
            }

            if (missingItemsTexts.Count > 0)
            {
                result.AddError($"Missing item dependencies: {string.Join(", ", missingItemsTexts)}");
            }

            var cycle = LoaderUtils.FindCycle(setupItems, i => i.EnumerateEditorPossibleDirectDependencies(setupContext));
            if (cycle != null)
                result.AddError($"Cyclic dependency detected: {string.Join(" → ", cycle.Select(i => i.name))}");

            var duplicateItems = _LoadItems.Where(i => i != null)
                .GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key.name)
                .ToList();

            if (duplicateItems.Count > 0)
            {
                result.AddError($"There are duplicate items: {string.Join(", ", duplicateItems)}");
            }

            // Multiple items representing a type referenced by a TypedDependency resolve ambiguously at runtime.
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
