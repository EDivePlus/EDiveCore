// Author: František Holubec
// Created: 29.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEngine;
using UnityEngine.Localization;

namespace EDIVE.Localization.Editor.Sheets
{
    // Column found by the ID in its header cell note, the letter is resolved on every push/pull
    public interface IIdSheetColumn
    {
        string Id { get; }
        string Title { get; }
    }

    [Serializable]
    [TypeRegistryItem("Key")]
    public class IdKeyColumn : KeyColumn, IIdSheetColumn
    {
        [HideInInspector]
        [SerializeField]
        private string _Title;

        [ShowInInspector]
        [LabelText("Header")]
        public string Title
        {
            get => string.IsNullOrWhiteSpace(_Title) ? "Key" : _Title;
            set => _Title = value;
        }

        public string Id => "key";

        public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
        {
            header = Title;
            headerNote = Id;
        }
    }

    [Serializable]
    [TypeRegistryItem("Locale")]
    public class IdLocaleColumn : LocaleColumn, IIdSheetColumn
    {
        [HideInInspector]
        [SerializeField]
        private string _Title;

        [ShowInInspector]
        [LabelText("Header")]
        public string Title
        {
            get => string.IsNullOrWhiteSpace(_Title) ? LocaleIdentifier.ToString() : _Title;
            set => _Title = value == LocaleIdentifier.ToString() ? null : value;
        }

        public string Id => LocaleIdentifier.Code;

        public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
        {
            header = Title;
            headerNote = Id;
        }
    }

    [Serializable]
    [TypeRegistryItem("Key Comment")]
    public class IdKeyCommentColumn : KeyCommentColumn, IIdSheetColumn
    {
        [HideInInspector]
        [SerializeField]
        private string _Title;

        [ShowInInspector]
        [LabelText("Header")]
        public string Title
        {
            get => string.IsNullOrWhiteSpace(_Title) ? "Key Comment" : _Title;
            set => _Title = value;
        }

        public string Id => "key-comment";

        public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
        {
            header = Title;
            headerNote = Id;
        }
    }

    [Serializable]
    [TypeRegistryItem("Locale Comment")]
    public class IdLocaleCommentColumn : LocaleCommentColumn, IIdSheetColumn
    {
        [HideInInspector]
        [SerializeField]
        private string _Title;

        [ShowInInspector]
        [LabelText("Header")]
        public string Title
        {
            get => string.IsNullOrWhiteSpace(_Title) ? $"{LocaleIdentifier} Comment" : _Title;
            set => _Title = value == $"{LocaleIdentifier} Comment" ? null : value;
        }

        public string Id => $"{LocaleIdentifier.Code}-comment";

        public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
        {
            header = Title;
            headerNote = Id;
        }
    }

    public class IdSheetColumnAttributeProcessor<T> : OdinAttributeProcessor<T> where T : SheetColumn, IIdSheetColumn
    {
        // The letter is resolved from the ID, comments toggle goes last
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            if (member.Name == "m_Column")
                attributes.Add(new HideInInspector());
            else if (member.Name == "m_IncludeComments")
                attributes.Add(new PropertyOrderAttribute(10));
        }
    }

    // Columns in a list get the type dropdown with the type's fields below, like any other polymorphic list
    public class SheetColumnTypeSelectorProcessor : OdinAttributeProcessor<SheetColumn>
    {
        public override bool CanProcessSelfAttributes(InspectorProperty property) => property.Parent?.ChildResolver is ICollectionResolver;

        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes) =>
            attributes.Add(new EnhancedTypeSelectorAttribute(true, 1, true));
    }

    public static class IdSheetColumnUtility
    {
        public static List<SheetColumn> CreateDefaultColumns()
        {
            var columns = new List<SheetColumn> {new IdKeyColumn {ColumnIndex = 0}};
            foreach (var locale in LocalizationEditorSettings.GetLocales())
                columns.Add(new IdLocaleColumn {LocaleIdentifier = locale.Identifier, ColumnIndex = columns.Count});
            return columns;
        }

        public static List<SheetColumn> Clone(IEnumerable<SheetColumn> columns) =>
            columns.Where(c => c != null).Select(c => (SheetColumn) JsonUtility.FromJson(JsonUtility.ToJson(c), c.GetType())).ToList();

        // Letter columns to ID columns, header text already in the sheet is kept
        public static SheetColumn ToIdColumn(SheetColumn column, IList<SheetHeader> headers)
        {
            var title = headers != null && column.ColumnIndex < headers.Count ? headers[column.ColumnIndex].Title : null;
            SheetColumn result = column switch
            {
                IIdSheetColumn => column,
                KeyColumn => new IdKeyColumn {Title = title},
                LocaleColumn locale => new IdLocaleColumn {LocaleIdentifier = locale.LocaleIdentifier, IncludeComments = locale.IncludeComments, Title = title},
                KeyCommentColumn => new IdKeyCommentColumn {Title = title},
                LocaleCommentColumn comment => new IdLocaleCommentColumn {LocaleIdentifier = comment.LocaleIdentifier, Title = title},
                _ => column
            };
            result.ColumnIndex = column.ColumnIndex;
            return result;
        }

        // Places ID columns by the header note, header text is the fallback for sheets without notes yet.
        // Pull drops columns missing in the sheet, push appends them after the last used column.
        public static List<SheetColumn> Resolve(IList<SheetColumn> columns, IList<SheetHeader> headers, bool forPush, string context)
        {
            var byNote = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var byTitle = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                if (!string.IsNullOrEmpty(headers[i].Note))
                    byNote.TryAdd(headers[i].Note, i);
                if (!string.IsNullOrEmpty(headers[i].Title))
                    byTitle.TryAdd(headers[i].Title, i);
            }

            var result = new List<SheetColumn>();
            var used = new HashSet<int>(columns.Where(c => c is not IIdSheetColumn && !string.IsNullOrEmpty(c.Column)).Select(c => c.ColumnIndex));
            var missing = new List<SheetColumn>();
            foreach (var column in columns)
            {
                if (column is not IIdSheetColumn idColumn)
                {
                    if (!string.IsNullOrEmpty(column.Column))
                        result.Add(column);
                    else
                        Debug.LogWarning($"[Localization] {context}: {column.GetType().Name} has no column letter, skipped");
                    continue;
                }

                if ((byNote.TryGetValue(idColumn.Id.Trim(), out var index) || byTitle.TryGetValue(idColumn.Title.Trim(), out index)) && used.Add(index))
                {
                    column.ColumnIndex = index;
                    result.Add(column);
                }
                else
                {
                    missing.Add(column);
                }
            }

            if (forPush)
            {
                var next = Math.Max(headers.Count, used.Count > 0 ? used.Max() + 1 : 0);
                foreach (var column in missing)
                {
                    while (used.Contains(next)) next++;
                    column.ColumnIndex = next;
                    used.Add(next);
                    result.Add(column);
                }
            }
            else
            {
                foreach (var column in missing)
                    Debug.LogWarning($"[Localization] {context}: column '{((IIdSheetColumn) column).Id}' not found in the sheet, skipped");
            }

            return result;
        }
    }

    public readonly struct SheetHeader
    {
        public readonly string Title;
        public readonly string Note;

        public SheetHeader(string title, string note)
        {
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }
    }
}
