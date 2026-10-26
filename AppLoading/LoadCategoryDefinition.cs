// Author: František Holubec
// Created: 22.10.2026

using EDIVE.AssetTranslation;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using EDIVE.AppLoading.Dependencies;
using EDIVE.AppLoading.LoadItems;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using JetBrains.Annotations;
#endif

namespace EDIVE.AppLoading
{
    public class LoadCategoryDefinition : AUniqueDefinition
    {
#if UNITY_EDITOR
        [PropertySpace]
        [ShowInInspector]
        [Searchable]
        [ReadOnlyListElements]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "ItemsTitleBarGUI")]
        public List<LoadItemDefinition> Items { get; private set; }

        [ShowInInspector]
        [Searchable]
        [ReadOnlyListElements]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "DependentByTitleBarGUI")]
        public List<LoadItemDefinition> DependentBy { get; private set; }

        [UsedImplicitly]
        private void ItemsTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
                ResolveItems();
        }

        [UsedImplicitly]
        private void DependentByTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
                ResolveDependentBy();
        }

        [OnInspectorInit]
        public void ResolveItems()
        {
            Items = DependencyResolutionContext.EditorContext.GetItemsInCategory(this).ToList();
        }

        [OnInspectorInit]
        public void ResolveDependentBy()
        {
            DependentBy = DependencyResolutionContext.EditorContext.AllItems.Where(i => i.CategoryDependencies.Contains(this)).ToList();
        }

        protected override string FormatFileNameForID(string filename) => filename.Replace("Definition", "").Replace("LoadCategory", "");
#endif
    }
}
