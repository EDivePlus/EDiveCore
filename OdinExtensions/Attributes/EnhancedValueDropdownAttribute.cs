using System;

namespace EDIVE.OdinExtensions.Attributes
{
#pragma warning disable
    
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public class EnhancedValueDropdownAttribute : Attribute
    {
        public string ValuesGetter;

        public int NumberOfItemsBeforeEnablingSearch;

        public bool IsUniqueList;

        public bool DrawDropdownForListElements;

        public bool DisableListAddButtonBehaviour;

        public bool ExcludeExistingValuesInList;

        public bool ExpandAllMenuItems;

        public bool AppendNextDrawer;

        public bool DisableGUIInAppendedDrawer;

        public bool DoubleClickToConfirm;

        public bool FlattenTreeView;

        public int DropdownWidth;

        public int DropdownHeight;

        public string DropdownTitle;

        public bool SortDropdownItems;

        public bool CopyValues = true;

        public bool OnlyChangeValueOnConfirm = false;
        
        public string ShowIf;

        public string IconGetter;

        public string ValueLabelGetter;

        public bool DrawThumbnailIcon;

        public bool OverrideExistingValues;
        
        public string OnListEndGUI;

        public int ChildrenIndent;

        public bool DontReloadOnInit;

        public DropdownChildrenDisplayType ChildrenDisplayType;

        public EnhancedValueDropdownAttribute(string valuesGetter, bool appendNextDrawer = false)
        {
            NumberOfItemsBeforeEnablingSearch = 10;
            ValuesGetter = valuesGetter;
            DrawDropdownForListElements = true;
            AppendNextDrawer = appendNextDrawer;
        }
    }

    public enum DropdownChildrenDisplayType
    {
        ShowChildrenInline,
        ShowChildrenInFoldout,
        HideChildren,
    }
}
