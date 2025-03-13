#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.External.SelectionHistory
{
    public class SelectionHistoryWindow : OdinEditorWindow
    {
        [ShowInInspector]
        public bool EnableSelectionHistory
        {
            get => SelectionHistoryManager.EnableSelectionHistory; 
            set => SelectionHistoryManager.EnableSelectionHistory = value;
        }
        
        [ShowInInspector]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false, DraggableItems = false, HideAddButton = true, OnTitleBarGUI = nameof(OnHistoryTitleBarGUI))]
        [CustomValueDrawer(nameof(CustomSelectionSnapshotDrawer))]
        private List<SelectionSnapshot> _history = new List<SelectionSnapshot>();

        private int _current;

        [MenuItem("Tools/Selection History")]
        private static void Open()
        {
            var window = GetWindow<SelectionHistoryWindow>();
            window.titleContent = new GUIContent("Selection History");
            window.Show();
        }

        protected override void Initialize()
        {
            base.Initialize();
            Refresh();
            SelectionHistoryManager.HistoryChanged += Refresh;
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            SelectionHistoryManager.HistoryChanged -= Refresh;
        }

        private void Refresh()
        {
            _history = SelectionHistoryManager.History.ToArray().Reverse().ToList();
            _current = SelectionHistoryManager.History.Size - SelectionHistoryManager.History.GetCurrentArrayIndex() - 1;
        }
        
        private void OnHistoryTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.ArrowLeftSolid))
            {
                SelectionHistoryManager.Back();
            }
            
            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.ArrowRightSolid))
            {
                SelectionHistoryManager.Forward();
            }
            
            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.BrushSolid))
            {
                SelectionHistoryManager.Clear();
            }
        }
        
        private void SetSelected(int index)
        {
            SelectionHistoryManager.HideSelectionFromHistory();
            SelectionHistoryManager.Select(SelectionHistoryManager.Size - index - 1);
        }
        
        private SelectionSnapshot CustomSelectionSnapshotDrawer(SelectionSnapshot value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            var previousBgColor = GUI.backgroundColor;
            var currentIndex = _history.IndexOf(value);
            if (currentIndex == _current)
            {
                GUI.backgroundColor = Color.green;
            }
            SirenixEditorGUI.BeginBox();
            GUI.backgroundColor = previousBgColor;
            EditorGUILayout.BeginHorizontal();
            
            var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));
            if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.CrosshairsSolid, "Select"))
            {
                SetSelected(currentIndex);
            }
            callNextDrawer(label);
            EditorGUILayout.EndHorizontal();
            SirenixEditorGUI.EndBox();
            return value;
        }
    }
}
#endif
