// Author: František Holubec
// Created: 14.04.2025

#if UNITY_EDITOR
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using NamedValue = Sirenix.OdinInspector.Editor.ValueResolvers.NamedValue;

namespace EDIVE.AssetTranslation
{
    [DrawerPriority(DrawerPriorityLevel.ValuePriority)]
    public sealed class UniqueDefinitionIDAttributeDrawer : OdinAttributeDrawer<UniqueDefinitionIDAttribute, string>
    {
        private const string FILENAME_ID = "filename";

        private bool _isInScriptableObject;
        private ScriptableObject _parentScriptableObject;

        private ADefinitionTranslator _translator;
        private ValueResolver<string> _formatIDMethodResolver;

        protected override void Initialize()
        {
            base.Initialize();
            _isInScriptableObject = Property.TryGetParentObject(out _parentScriptableObject);
            if (Attribute.FormatFileNameMethod != null)
                _formatIDMethodResolver = ValueResolver.Get<string>(Property, Attribute.FormatFileNameMethod,
                    new NamedValue(FILENAME_ID, typeof(string)));
        }

        /// <summary>Not yet documented.</summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_formatIDMethodResolver);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            var newValue = SirenixEditorFields.DelayedTextField(label, ValueEntry.SmartValue);
            if (EditorGUI.EndChangeCheck() && !ValueEntry.SmartValue.Equals(newValue))
            {
                Property.Tree.DelayActionUntilRepaint(() => TrySetValue(newValue));
            }

            if (_isInScriptableObject)
            {
                var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
                if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.FilePenSolid, "Use filename as ID"))
                {
                    Property.Tree.DelayActionUntilRepaint(() =>
                    {
                        var name = _parentScriptableObject.name;
                        if (_formatIDMethodResolver != null && !_formatIDMethodResolver.HasError)
                        {
                            _formatIDMethodResolver.Context.NamedValues.Set(FILENAME_ID, name);
                            name = _formatIDMethodResolver.GetValue();
                            Property.MarkSerializationRootDirty();
                        }
                        TrySetValue(name);
                    });
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void TrySetValue(string newValue)
        {
            if (newValue == ValueEntry.SmartValue)
                return;

            if (string.IsNullOrWhiteSpace(ValueEntry.SmartValue))
            {
                SetValue(newValue);
                return;
            }

            if (TryGetTranslator(out var translator))
            {
                var option = EditorUtility.DisplayDialogComplex("Override ID?",
                    $"You are trying to override ID from {ValueEntry.SmartValue} to {newValue}.\nDo you want to also add it to migration setup?",
                    "Only Save", "Cancel", "Save and Migrate");

                if (option == 0) // Only Save
                {
                    SetValue(newValue);
                }
                else if (option == 2) // Save and Migrate
                {
                    if (translator.TryAddMigrationPreset(ValueEntry.SmartValue, _parentScriptableObject))
                    {
                        SetValue(newValue);
                    }
                    else if (EditorUtility.DisplayDialog("Migration failed!", "Could not add migration preset! Do you still want to override ID?",
                                 "Continue", "Cancel"))
                    {
                        SetValue(newValue);
                    }
                }
            }
            else
            {
                if (EditorUtility.DisplayDialog("Override ID?", $"You are trying to override ID from {ValueEntry.SmartValue} to {newValue}.",
                        "Continue", "Cancel")) // Only Save
                {
                    SetValue(newValue);
                }
            }
        }

        private void SetValue(string newValue)
        {
            Property.RecordForUndo("Set new value");
            ValueEntry.SmartValue = newValue;
            Property.MarkSerializationRootDirty();
        }

        private bool TryGetTranslator(out ADefinitionTranslator translator)
        {
            return AssetTranslationConfig.Instance.TryGetTranslator(_parentScriptableObject.GetType(), out translator);
        }
    }
}
#endif
