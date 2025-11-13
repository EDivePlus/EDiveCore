// Author: František Holubec
// Created: 13.11.2025

using System;
using System.Linq;

namespace EDIVE.OdinExtensions.Attributes
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public class EnhancedAssetSelectorAttribute : Attribute
    {
        public bool IsUniqueList = true;
        public bool DrawDropdownForListElements = true;
        public bool DisableListAddButtonBehaviour;
        public bool ExcludeExistingValuesInList;
        public bool ExpandAllMenuItems = true;
        public bool FlattenTreeView;
        public int DropdownWidth;
        public int DropdownHeight;
        public string DropdownTitle;
        public string[] SearchInFolders;
        public string Filter;
        
        public string Paths
        {
            set
            {
                SearchInFolders = value.Split('|')
                    .Select(x => x.Trim().Trim('/', '\\'))
                    .ToArray();
            }
            get => SearchInFolders == null ? null : string.Join(",", this.SearchInFolders);
        }
    }
}
