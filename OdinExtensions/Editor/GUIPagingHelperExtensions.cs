using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public static class GUIPagingHelperExtensions
    {
        public static void DrawToolbarPagingButtons(this GUIPagingHelper paging, bool showPaging, bool showItemCount, int btnWidth = 23)
        {
            EditorGUILayout.BeginHorizontal();
            var drawPaging = paging.IsEnabled && !paging.IsExpanded && showPaging && paging.PageCount > 1; 
            var drawExpand = paging.IsEnabled && paging.PageCount > 1; 
            var drawPagingField = drawPaging; 

            // Item Count
            if (showItemCount)
            {
                var lbl = new GUIContent(paging.ElementCount == 0 ? "Empty" : $"{paging.ElementCount} items");
                var width = SirenixGUIStyles.LeftAlignedGreyMiniLabel.CalcSize(lbl).x + 5;
                EditorGUILayout.LabelField(lbl, SirenixGUIStyles.LeftAlignedGreyMiniLabel, GUILayout.Width(width));
            }
            
            // Left
            if (drawPaging)
            {
                if (SirenixEditorGUI.ToolbarButton(EditorIcons.TriangleLeft))
                {
                    if (Event.current.button != 1)
                    {
                        paging.CurrentPage = PositiveModulo(paging.CurrentPage - 1, paging.PageCount);
                    }
                    else
                    {
                        paging.CurrentPage = 0;
                    }
                }
            }

            // Paging field
            if (drawPagingField)
            {
                var pageCountLbl = $"/ {paging.PageCount}";
                var lblLength = SirenixGUIStyles.Label.CalcSize(new GUIContent(pageCountLbl)).x;
                
                var next = EditorGUILayout.IntField(paging.CurrentPage + 1, GUILayout.Width(lblLength)) - 1;
                if (next != paging.CurrentPage) 
                    paging.CurrentPage = next;
                EditorGUILayout.LabelField(pageCountLbl, SirenixGUIStyles.LabelCentered, GUILayout.Width(lblLength));
            }

            // Right
            if (drawPaging)
            {
                if (SirenixEditorGUI.ToolbarButton(EditorIcons.TriangleRight))
                {
                    if (Event.current.button == 1)
                    {
                        paging.CurrentPage = paging.PageCount - 1;
                    }
                    else
                    {
                        paging.CurrentPage = PositiveModulo(paging.CurrentPage + 1, paging.PageCount);
                    }
                }
            }
            
            // Expand
            if (drawExpand)
            {
                if (SirenixEditorGUI.ToolbarButton(paging.IsExpanded ? EditorIcons.TriangleUp : EditorIcons.TriangleDown))
                {
                    paging.IsExpanded = !paging.IsExpanded;
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        
        private static int PositiveModulo(int x, int m)
        {
            var r = x % m;
            return r < 0 ? r + m : r;
        } 
    }
}
