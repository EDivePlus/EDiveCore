// Author: František Holubec
// Created: 29.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.OdinExtensions.Editor;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace EDIVE.Localization.Editor.Sheets
{
    // Replaces the package inspector, the collection is drawn through its facade
    [CustomEditor(typeof(StringTableCollection))]
    public class StringTableCollectionOdinEditor : OdinEditor
    {
    }

    [UsedImplicitly]
    public class StringTableCollectionFacade : OdinFacade<StringTableCollection>
    {
        [FacadeMember]
        private string Group { get; set; }

        [ShowInInspector]
        [PropertyOrder(1)]
        [LabelText("Tables")]
        [OnStateUpdate(nameof(SyncTables))]
        [ListDrawerSettings(ShowFoldout = false, DraggableItems = false, CustomAddFunction = nameof(ShowAddTableMenu), CustomRemoveElementFunction = nameof(RemoveTable),
            OnTitleBarGUI = nameof(DrawTablesTitleBar))]
        private List<TableEntry> _tables = new();

        [ShowInInspector]
        [PropertyOrder(2)]
        [LabelText("Extensions")]
        [OnStateUpdate(nameof(SyncExtensions))]
        [ListDrawerSettings(ShowFoldout = false, DraggableItems = false, CustomAddFunction = nameof(ShowAddExtensionMenu), CustomRemoveElementFunction = nameof(RemoveExtension),
            OnTitleBarGUI = nameof(DrawExtensionsTitleBar))]
        private List<CollectionExtension> _extensions = new();

        public static IEnumerable<Type> ExtensionTypes => TypeCache.GetTypesWithAttribute<StringTableCollectionExtensionAttribute>()
            .Where(t => !t.IsAbstract && typeof(CollectionExtension).IsAssignableFrom(t));

        // Lists change only on layout, a count change in repaint breaks the list layout
        private static bool IsLayout => Event.current == null || Event.current.type == EventType.Layout;

        private void SyncTables()
        {
            if (!IsLayout)
                return;

            var tables = Target.StringTables.Where(t => t != null).ToList();
            if (_tables.Select(e => e.Table).SequenceEqual(tables))
                return;

            _tables.Clear();
            _tables.AddRange(tables.Select(t => new TableEntry(t)));
        }

        // A type picked in the selector replaces the element, it has to go through the collection to get its target set
        private void SyncExtensions()
        {
            if (!IsLayout)
                return;

            var current = Target.Extensions;
            for (var i = 0; i < _extensions.Count && i < current.Count; i++)
            {
                if (_extensions[i] == current[i])
                    continue;

                Undo.RecordObject(Target, "Change Extension Type");
                var next = _extensions[i];
                Target.RemoveExtension(current[i]);
                if (next != null)
                    Target.AddExtension(next);
                EditorUtility.SetDirty(Target);
                break;
            }

            if (_extensions.SequenceEqual(Target.Extensions))
                return;

            _extensions.Clear();
            _extensions.AddRange(Target.Extensions);
        }

        private void DrawTablesTitleBar()
        {
            if (OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.TableSolid, "Open in Localization Editor"))
                OdinLocalizationWindowUtility.Open(Target);
        }

        private void DrawExtensionsTitleBar()
        {
            if (Target.Extensions.Any(e => e is GoogleSheetsExtension and not EnhancedGoogleSheetsExtension) &&
                SirenixEditorGUI.ToolbarButton(new GUIContent("Convert Sheets", "Swap legacy Google Sheets extensions for enhanced ones")))
                GoogleSheetsSync.ConvertToEnhancedAsync(new[] {Target}).Forget();
        }

        private void ShowAddTableMenu()
        {
            var menu = new GenericMenu();
            foreach (var locale in LocalizationEditorSettings.GetLocales().Where(l => !Target.ContainsTable(l.Identifier)))
                menu.AddItem(new GUIContent(locale.LocaleName), false, () => Target.AddNewTable(locale.Identifier));
            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("Every locale has a table"));
            menu.ShowAsContext();
        }

        private void RemoveTable(TableEntry entry) => Target.RemoveTable(entry.Table, createUndo: true);

        private void ShowAddExtensionMenu()
        {
            var menu = new GenericMenu();
            foreach (var type in ExtensionTypes)
            {
                menu.AddItem(new GUIContent(GetExtensionName(type)), false, () =>
                {
                    Undo.RecordObject(Target, "Add Extension");
                    Target.AddExtension((CollectionExtension) Activator.CreateInstance(type));
                    EditorUtility.SetDirty(Target);
                });
            }

            menu.ShowAsContext();
        }

        private void RemoveExtension(CollectionExtension extension)
        {
            Undo.RecordObject(Target, "Remove Extension");
            Target.RemoveExtension(extension);
            EditorUtility.SetDirty(Target);
        }

        private static string GetExtensionName(Type type)
        {
            var attribute = type.GetAttribute<DisplayNameAttribute>();
            return attribute != null ? attribute.Name : type.Name.Replace("Extension", "").SplitPascalCase();
        }

        [InlineProperty]
        [HideReferenceObjectPicker]
        public class TableEntry
        {
            private string Locale => Table != null ? Table.LocaleIdentifier.CultureInfo?.EnglishName ?? Table.LocaleIdentifier.Code : "";

            [ShowInInspector]
            [LabelText("$" + nameof(Locale))]
            [ReadOnly]
            public StringTable Table { get; }

            public TableEntry(StringTable table) => Table = table;
        }
    }

    // Extensions use the type selector look, fields drawn inline below the dropdown
    public class CollectionExtensionTypeSelectorProcessor : OdinAttributeProcessor<CollectionExtension>
    {
        public override bool CanProcessSelfAttributes(InspectorProperty property) =>
            property.Parent?.Parent?.ValueEntry?.TypeOfValue == typeof(StringTableCollectionFacade);

        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            attributes.Add(new EnhancedTypeSelectorAttribute(true, 1, true)
            {
                CustomTypesGetter = "@" + typeof(StringTableCollectionFacade).FullName + "." + nameof(StringTableCollectionFacade.ExtensionTypes)
            });
        }
    }
}
