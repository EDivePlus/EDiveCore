// Author: František Holubec
// Created: 29.10.2026

using System.Linq;
using Sirenix.OdinInspector.Modules.Localization.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace EDIVE.Localization.Editor
{
    public static class OdinLocalizationWindowUtility
    {
        public static void Open(StringTableCollection collection, SharedTableData.SharedTableEntry entry = null)
        {
            OdinLocalizationEditorWindow.OpenFromMenu();
            var window = EditorWindow.GetWindow<OdinLocalizationEditorWindow>();
            if (collection == null)
                return;

            if (window.MenuTree == null)
                window.ForceMenuTreeRebuild();

            var item = window.MenuTree?.EnumerateTree().FirstOrDefault(i => i.Value is OdinStringTableCollectionEditor e && e.Collection == collection);
            if (item == null)
                return;

            item.Select();
            if (entry != null && item.Value is OdinStringTableCollectionEditor editor)
                editor.SelectSharedEntry(entry);
        }
    }
}
