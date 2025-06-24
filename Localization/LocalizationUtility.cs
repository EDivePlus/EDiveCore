using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;

namespace EDIVE.Localization
{
    public static class LocalizationUtility
    {
        public static void SetTerm(this LocalizedString localizedString, string term)
        {
            if (string.IsNullOrEmpty(term))
                return;
            var tokens = term.Split('/', 2);
            if (tokens.Length == 2)
            {
                localizedString.TableReference = tokens[0];
                localizedString.TableEntryReference = tokens[1];
            }
        }
        
        public static void SetTerm(this LocalizedString localizedString, TableReference table, TableEntryReference entry)
        {
            localizedString.TableReference = table;
            localizedString.TableEntryReference = entry;
        }

        public static void SetTerm(this LocalizedString localizedString, string table, string entry)
        {
            localizedString.TableReference = table;
            localizedString.TableEntryReference = entry;
        }

        public static string GetTerm(this LocalizedString localizedString)
        {
            if (localizedString.IsEmpty)
                return null;

            if (localizedString.TableEntryReference.ReferenceType == TableEntryReference.Type.Name)
                return $"{localizedString.TableReference.TableCollectionName}/{localizedString.TableEntryReference.Key}";

            SharedTableData sharedTableData;
#if !UNITY_WEBGL
            var stringDatabase = LocalizationSettings.StringDatabase;
            if (stringDatabase == null)
                return null;

            var loadOperation = stringDatabase.GetTableAsync(localizedString.TableReference);
            if (!loadOperation.IsDone)
                return null;

            sharedTableData = loadOperation.Result?.SharedData;
#else
            try
            {
                sharedTableData = LocalizationSettings.StringDatabase?.GetTable(localizedString.TableReference)?.SharedData;

            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
#endif
            return $"{localizedString.TableReference.TableCollectionName}/{localizedString.TableEntryReference.ResolveKeyName(sharedTableData)}";
        }

        public static void SetParameter<TValue, TVariable>(this LocalizedString localizedString, string paramName, TValue paramValue)
            where TVariable : Variable<TValue>, new()
        {
            if (localizedString.TryGetValue(paramName, out var existingValue) && existingValue is TVariable typedExistingValue)
            {
                typedExistingValue.Value = paramValue;
            }
            else
            {
                localizedString.Add(paramName, new TVariable()
                {
                    Value = paramValue
                });
            }
        }
        
        public static string WithLocalizedParameters(this LocalizedString localizedString, IDictionary<string, string> parameters)
        {
            if (localizedString.IsEmpty)
                return "";
            foreach (var p in parameters)
            {
                localizedString.Add(p.Key, new StringVariable()
                {
                    Value = p.Value
                });
            }

#if UNITY_WEBGL
            var loadOperation = localizedString.GetLocalizedStringAsync();
            if (!loadOperation.IsDone)
            {
                return UnityLocalizedString.NOT_LOADED_TERM;
            }
            return loadOperation.Result;
#else
            return localizedString.GetLocalizedString();
#endif
        }

        public static SafeLocalizedString GetSafe(this LocalizedString localizedString)
        {
            return !localizedString.IsEmpty ? new SafeLocalizedString(localizedString.TableReference, localizedString.TableEntryReference) : SafeLocalizedString.UNDEFINED;
        }

        public static string GetLocalizedStringSafe(this LocalizedString localizedString, string fallback = null)
        {
            if (localizedString.IsEmpty)
                return fallback ?? SafeLocalizedString.UNDEFINED_TERM;

#if UNITY_WEBGL
            var loadOperation = localizedString.GetLocalizedStringAsync();
            if (!loadOperation.IsDone)
            {
                return UnityLocalizedString.NOT_LOADED_TERM;
            }
            return loadOperation.Result;
#else
            return localizedString.GetLocalizedString();
#endif
        }

        public static string GetNativeName(this Locale locale)
        {
            return locale.Identifier.CultureInfo.NativeName;
        }

        public static string GetEnglishName(this Locale locale)
        {
            return locale.Identifier.CultureInfo.EnglishName;
        }

        public static LocalizedString WithLocaleOverride(this LocalizedString value, Locale locale)
        {
            return new LocalizedString
            {
                TableReference = value.TableReference,
                TableEntryReference = value.TableEntryReference,
                LocaleOverride = locale
            };
        }

        public static SafeLocalizedString WithLocaleOverride(this SafeLocalizedString value, Locale locale)
        {
            return new SafeLocalizedString
            {
                TableReference = value.TableReference,
                TableEntryReference = value.TableEntryReference,
                LocaleOverride = locale
            };
        }
    }
}
