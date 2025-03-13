using System;
using System.Diagnostics;
#pragma warning disable

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    [Conditional("UNITY_EDITOR")]
    public class EnhancedTableListAttribute : Attribute
    {

        public bool IsReadOnly;

        public int DefaultMinColumnWidth = 40;

        public bool ShowIndexLabels;
    
        public bool DrawScrollView = true;
    
        public int MinScrollViewHeight = 350;
    
        public int MaxScrollViewHeight;
    
        public bool AlwaysExpanded;
    
        public bool HideToolbar = false;
    
        public int CellPadding = 2;
        
        public string CustomAddFunction;
        
        public string CustomRemoveFunction;

        public bool HideAddButton;

        private bool _showPaging = false;
        public bool ShowPaging
        {
            get => _showPaging;
            set
            {
                _showPaging = value;
                ShowPagingHasValue = true;
            }
        }

        private int _numberOfItemsPerPage;
        public int NumberOfItemsPerPage
        {
            get => _numberOfItemsPerPage;
            set
            {
                _numberOfItemsPerPage = value;
                ShowPaging = true;
            }
        }

        public string OnTitleBarGUI;
    
        public bool ShowPagingHasValue { get; private set; }

        public int ScrollViewHeight
        {
            get => Math.Min(MinScrollViewHeight, MaxScrollViewHeight);
            set => MinScrollViewHeight = MaxScrollViewHeight = value;
        }

    }
}
