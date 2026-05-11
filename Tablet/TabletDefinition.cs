// Author: Michal Petr
// Created: 03.03.2026

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.AssetTranslation;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Tablet
{
    public class TabletDefinition : AUniqueDefinition
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        [EnhancedValueDropdown("GetAvailableWidgets", AppendNextDrawer = true, IsUniqueList = true)]
        private List<TabletWidgetDefinition> _Widgets = new();

        [SerializeField]
        [EnhancedValueDropdown("GetAvailableWidgets", AppendNextDrawer = true)]
        private TabletWidgetDefinition _DefaultWidget;
        
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        [EnhancedValueDropdown("GetAvailableWidgets", AppendNextDrawer = true, IsUniqueList = true)]
        private List<TabletWidgetDefinition> _PinnedWidgets = new();

        public TabletWidgetDefinition DefaultWidget => _DefaultWidget;
        public IReadOnlyList<TabletWidgetDefinition> Widgets => _Widgets;
        public IReadOnlyList<TabletWidgetDefinition> PinnedWidgets => _PinnedWidgets;

#if UNITY_EDITOR
        private IEnumerable GetAvailableWidgets()
        {
            return _Widgets.Where(w => w).Select(w => new ValueDropdownItem<TabletWidgetDefinition>(w.UniqueID, w));
        }
        
        private IEnumerable GetAllWidgets()
        {
            return EditorUtils.EditorAssetUtils.FindAllAssetsOfType<TabletWidgetDefinition>();
        }
#endif
    }
}
