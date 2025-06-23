using System;
using System.ComponentModel;
using System.Linq;
using EDIVE.Localization.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Core.Parsing;

[assembly: RegisterValidationRule(typeof(LocalizationSmartValuesValidator), "Validate Localization Smart Values")]
namespace EDIVE.Localization.Editor
{
    public class LocalizationSmartValuesValidator: RootObjectValidator<StringTableCollection>
    {
        protected override void Validate(ValidationResult result)
        {
            result.Setup.Validator = this;

            var projectLocaleIdentifier = LocalizationSettings.ProjectLocale?.Identifier ?? new LocaleIdentifier("en");

            foreach (var row in Object.GetRowEnumerator())
            {
                var key = row.KeyEntry.Key;

                var canonicalEntry = row.TableEntries.FirstOrDefault(e => e != null && e.Table.LocaleIdentifier == projectLocaleIdentifier);
                var canonicalParameters = canonicalEntry?.Value != null ? ParseParameters(canonicalEntry.Value) : Array.Empty<string>();
                var expectedParametersString = string.Join(", ", canonicalParameters);
                
                foreach (var entry in row.TableEntries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
                        continue;
                    
                    var parameters = ParseParameters(entry.Value);
                    
                    if (!DoParametersMatch(parameters, canonicalParameters))
                    {
                        var item = GetError("Localization entry smart value parameters do not match between columns");
                        item = AppendEntryMetaData(ref item);
                        item = item.WithMetaData("Expected parameters", expectedParametersString, ReadOnlyAttribute.Yes);
                        result.Add(item);
                    }
                    
                    if (!entry.IsSmart && parameters.Length > 0)
                    {
                        var item = GetError("Localization entry not marked smart, but contains smart value parameters");
                        item = item.WithFix("Mark as smart", () =>
                        {
                            entry.IsSmart = true;
                            EditorUtility.SetDirty(entry.Table);
                        });
                        item = AppendEntryMetaData(ref item);
                        result.Add(item);
                    }
                    else if (entry.IsSmart && parameters.Length < 1)
                    {
                        var item = GetError("Localization entry marked smart, but doesn't contain any smart value parameters");
                        item = item.WithFix("Unmark as smart", () =>
                        {
                            entry.IsSmart = false;
                            EditorUtility.SetDirty(entry.Table);
                        });
                        item = AppendEntryMetaData(ref item);
                        result.Add(item);
                    }

                    ResultItem GetError(string message) => new(message, ValidationResultType.Error);

                    ref ResultItem AppendEntryMetaData(ref ResultItem item)
                    {
                        item = item.WithMetaData("Locale", entry.Table.LocaleIdentifier.CultureInfo.EnglishName, ReadOnlyAttribute.Yes)
                            .WithMetaData("Key", key, ReadOnlyAttribute.Yes)
                            .WithMetaData("Value", entry.Value)
                            .WithMetaData("Parameters", string.Join(", ", parameters), ReadOnlyAttribute.Yes);
                        return ref item;
                    }
                }
            }
        }

        private bool DoParametersMatch(string[] parameters, string[] expectedParameters)
        {
            if (parameters.Length != expectedParameters.Length)
                return false;

            for (var i = 0; i < parameters.Length; i++)
                if (parameters[i] != expectedParameters[i])
                    return false;
            return true;
        }

        private string[] ParseParameters(string value)
        {
            var formater = LocalizationSettings.StringDatabase.SmartFormatter;
            var parser = formater.Parser;

            var format = parser.ParseFormat(value, formater.GetNotEmptyFormatterExtensionNames());
            return format.Items.OfType<Placeholder>().SelectMany(p => p.Selectors).Select(s => s.RawText).OrderBy(v => v).ToArray();
        }
    }
}
