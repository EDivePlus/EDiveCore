// Author: Michal Petr
// Created: 03.03.2026

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.AssetTranslation;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.MenuScreen
{
    public class MenuScreenDefinition : AUniqueDefinition
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        [EnhancedValueDropdown("GetAllWidgets", AppendNextDrawer = true, IsUniqueList = true)]
        private List<WidgetDefinition> _Widgets = new();

        [SerializeField]
        [EnhancedValueDropdown("GetAvailableWidgets", AppendNextDrawer = true)]
        private WidgetDefinition _DefaultWidget;
        
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        [EnhancedValueDropdown("GetAvailableWidgets", AppendNextDrawer = true, IsUniqueList = true)]
        private List<WidgetDefinition> _PinnedWidgets = new();

        public WidgetDefinition DefaultWidget => _DefaultWidget;
        public IReadOnlyList<WidgetDefinition> Widgets => _Widgets;
        public IReadOnlyList<WidgetDefinition> PinnedWidgets => _PinnedWidgets;

#if UNITY_EDITOR
        private IEnumerable GetAvailableWidgets()
        {
            return _Widgets.Where(w => w).Select(w => new ValueDropdownItem<WidgetDefinition>(w.UniqueID, w));
        }
        
        private IEnumerable GetAllWidgets()
        {
            return EditorUtils.EditorAssetUtils.FindAllAssetsOfType<WidgetDefinition>();
        }
#endif
    }
}
