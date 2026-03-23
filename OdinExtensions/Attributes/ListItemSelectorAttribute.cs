using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    [Conditional("UNITY_EDITOR")]
    public class ListItemSelectorAttribute : Attribute
    {
        public string Select;
        public SelectorReloadActionType OnReloadAction;
        public string SetSelectedCallbackSetter;
        
        public bool HideSelectIcon;
        public EditorIconsBundle? SelectIconBundle;
        public string SelectIconName;
        
        public FontAwesomeEditorIconType SelectIconFa
        {
            set
            {
                SelectIconBundle = EditorIconsBundle.FontAwesome;
                SelectIconName = value.ToString();
            }
        }
        
        public ListItemSelectorAttribute(string select, SelectorReloadActionType onReloadAction = SelectorReloadActionType.SelectPreviousOrFirst)
        {
            Select = select;
            OnReloadAction = onReloadAction;
        }
    }
    
    public enum SelectorReloadActionType
    {
        None,
        SelectPrevious,
        SelectPreviousOrFirst,
        SelectFirst,
        Clear,
    }
}
