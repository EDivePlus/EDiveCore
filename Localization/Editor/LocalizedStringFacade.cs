// Author: František Holubec
// Created: 29.10.2026

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.OdinExtensions.Editor;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace EDIVE.Localization.Editor
{
    [UsedImplicitly]
    public class LocalizedStringFacade : OdinFacade<LocalizedString>
    {
        private const string NONE = "None";

        private StringTableCollection _valuesCollection;
        private long _valuesKeyId;

        [ShowInInspector]
        [ShowInGroupHeader]
        [HideLabel]
        [EnhancedValueDropdown(nameof(GetTerms), DontReloadOnInit = true)]
        [InlineIconButton(FontAwesomeEditorIconType.TableSolid, nameof(OpenEditor), "Open in Localization Editor")]
        private string Term
        {
            get
            {
                var collection = Collection;
                if (collection == null)
                    return Target.IsEmpty ? NONE : "Missing table";
                var entry = Entry;
                return entry != null ? $"{collection.TableCollectionName}/{entry.Key}" : $"{collection.TableCollectionName}/Missing key";
            }
            set
            {
                var collection = value == NONE ? null : LocalizationEditorSettings.GetStringTableCollections().FirstOrDefault(c => value.StartsWith(c.TableCollectionName + "/"));
                var entry = collection?.SharedData.GetEntry(value.Substring(collection.TableCollectionName.Length + 1));
                SetReference(collection, entry);
            }
        }

        [FacadeMember("FallbackState")]
        [LabelText("Fallback")]
        private FallbackBehavior Fallback { get; set; }

        [FacadeMember]
        private bool WaitForCompletion { get; set; }

        [ShowInInspector]
        [PropertyOrder(1)]
        [LabelText("Values")]
        [ShowIf(nameof(HasEntry))]
        [OnStateUpdate(nameof(RefreshValues))]
        [ListDrawerSettings(ShowFoldout = false, HideAddButton = true, HideRemoveButton = true, DraggableItems = false, OnTitleBarGUI = nameof(DrawValuesTitleBar))]
        private List<LocaleValue> _values = new();

        [FacadeMember("m_LocalVariables")]
        [PropertyOrder(3)]
        [ListDrawerSettings(ShowFoldout = false)]
        private object Variables { get; set; }

        private StringTableCollection Collection => Target.TableReference.ReferenceType == TableReference.Type.Empty
            ? null
            : LocalizationEditorSettings.GetStringTableCollection(Target.TableReference);

        private SharedTableData.SharedTableEntry Entry
        {
            get
            {
                var collection = Collection;
                return collection != null && Target.TableEntryReference.ReferenceType != TableEntryReference.Type.Empty
                    ? collection.SharedData.GetEntryFromReference(Target.TableEntryReference)
                    : null;
            }
        }

        private bool HasEntry => Entry != null;

        private static IEnumerable GetTerms()
        {
            yield return NONE;
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                foreach (var entry in collection.SharedData.Entries)
                    yield return $"{collection.TableCollectionName}/{entry.Key}";
            }
        }

        private void OpenEditor() => OdinLocalizationWindowUtility.Open(Collection, Entry);

        private void SetReference(StringTableCollection collection, SharedTableData.SharedTableEntry entry)
        {
            if (collection == null)
            {
                Target.TableReference = default;
                Target.TableEntryReference = default;
                return;
            }

            Target.TableReference = collection.SharedData.TableCollectionNameGuid;
            Target.TableEntryReference = entry != null ? entry.Id : default(TableEntryReference);
        }

        // Only on layout, a row count change in repaint breaks the list layout
        private void RefreshValues()
        {
            if (Event.current != null && Event.current.type != EventType.Layout)
                return;

            var collection = Collection;
            var entry = Entry;
            var keyId = entry?.Id ?? 0;
            var tableCount = entry != null ? collection.StringTables.Count(t => t != null) : 0;
            if (collection == _valuesCollection && keyId == _valuesKeyId && tableCount == _values.Count)
                return;

            _valuesCollection = collection;
            _valuesKeyId = keyId;
            _values.Clear();
            if (entry != null)
                _values.AddRange(collection.StringTables.Where(t => t != null).Select(t => new LocaleValue(t, keyId)));
        }

        private void DrawValuesTitleBar()
        {
            var smartCount = _values.Count(v => v.IsSmart);
            var icon = smartCount == _values.Count ? FontAwesomeEditorIcons.LightbulbOnSolid
                : smartCount > 0 ? FontAwesomeEditorIcons.LightbulbSolid
                : FontAwesomeEditorIcons.LightbulbRegular;
            if (!OdinExtensionUtils.ToolbarIconButton(icon, "Smart string for all locales"))
                return;

            var enable = smartCount != _values.Count;
            foreach (var value in _values)
                value.IsSmart = enable;
        }

        [InlineProperty]
        [HideReferenceObjectPicker]
        public class LocaleValue
        {
            private readonly StringTable _table;
            private readonly long _keyId;

            public LocaleValue(StringTable table, long keyId)
            {
                _table = table;
                _keyId = keyId;
            }

            private string Locale => LocalizationEditorSettings.GetLocale(_table.LocaleIdentifier)?.LocaleName ?? _table.LocaleIdentifier.Code;

            [ShowInInspector]
            [LabelText("$" + nameof(Locale))]
            [InlineIconButton(EditorIconsBundle.FontAwesome, "$" + nameof(SmartIcon), nameof(ToggleSmart), "Smart string")]
            private string Text
            {
                get => _table.GetEntry(_keyId)?.Value ?? "";
                set
                {
                    Undo.RecordObject(_table, "Edit Localized Value");
                    _table.AddEntry(_keyId, value);
                    EditorUtility.SetDirty(_table);
                }
            }

            public bool IsSmart
            {
                get => _table.GetEntry(_keyId)?.IsSmart == true;
                set
                {
                    Undo.RecordObject(_table, "Toggle Smart String");
                    var entry = _table.GetEntry(_keyId) ?? _table.AddEntry(_keyId, "");
                    entry.IsSmart = value;
                    EditorUtility.SetDirty(_table);
                }
            }

            private string SmartIcon => IsSmart ? nameof(FontAwesomeEditorIconType.LightbulbOnSolid) : nameof(FontAwesomeEditorIconType.LightbulbRegular);

            private void ToggleSmart() => IsSmart = !IsSmart;
        }
    }

    // Local variables: short name next to the value, type picked from a dropdown
    public class LocalizedStringVariableAttributeProcessor : OdinAttributeProcessor
    {
        private static readonly Type VARIABLE_PAIR = typeof(LocalizedString).Assembly.GetType("UnityEngine.Localization.SmartFormat.PersistentVariables.VariableNameValuePair");

        public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member) =>
            parentProperty.ValueEntry?.TypeOfValue == VARIABLE_PAIR;

        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            switch (member.Name)
            {
                case "name":
                    attributes.Add(new HorizontalGroupAttribute("Pair", 110));
                    attributes.Add(new HideLabelAttribute());
                    break;
                case "variable":
                    attributes.Add(new HorizontalGroupAttribute("Pair"));
                    attributes.Add(new HideLabelAttribute());
                    break;
            }
        }
    }
}
