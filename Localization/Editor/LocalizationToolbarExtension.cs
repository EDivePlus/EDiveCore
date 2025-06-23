using System.Collections.Generic;
using System.Linq;
using EDIVE.External.ToolbarExtensions;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace EDIVE.Localization.Editor
{
    public static class LocalizationToolbarExtension
    {
        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            ToolbarExtender.AddToRightToolbar(OnToolbarGUI, 95);
        }

        private static void OnToolbarGUI()
        {
            var activeLocalizationSettings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (!activeLocalizationSettings)
                return;
            
            GUILayout.Space(2);
            var dropdownRect = GUILayoutUtility.GetRect(0, 18).MinWidth(200);
            var currentLocale = activeLocalizationSettings.GetSelectedLocale();
            var code = currentLocale == null ? "--" : currentLocale.Identifier.Code.ToUpperInvariant();
            var content = new GUIContent($" {code}", FontAwesomeEditorIcons.LanguageSolid.Highlighted, "Refresh Language");
            if (GUILayout.Button(content, ToolbarStyles.ToolbarButton, GUILayout.Width(45)))
            {
                RefreshAll();
            }

            if (GUILayout.Button(new GUIContent(null, FontAwesomeEditorIcons.CaretDownSolid.Active, "Language Selector"), ToolbarStyles.ToolbarButton, GUILayout.Width(15)))
            {
                var locales = activeLocalizationSettings.GetAvailableLocales().Locales
                    .Prepend(null)
                    .Select(l => new LocaleWrapper(l));

                var selector = new GenericSelector<LocaleWrapper>(null, false, x => x.Name, locales);
                selector.SelectionTree.DefaultMenuStyle.Height = 22;
                selector.SelectionTree.Config.DrawSearchToolbar = true;
                selector.SelectionTree.Config.AutoFocusSearchBar = true;
                selector.EnableSingleClickToSelect();

                selector.SelectionConfirmed += selection =>
                {
                    var selected = selection.FirstOrDefault();
                    if (selected != null)
                    {
                        activeLocalizationSettings.SetSelectedLocale(selected.Locale);
                        ToolbarExtender.RepaintToolbar();
                        RefreshAll();
                    }
                };

                selector.ShowInPopup(dropdownRect);
            }
            GUILayout.Space(2);
        }

        private static void RefreshAll()
        {
            var stringEvents = GetAllLocalizeStringEvents();
            foreach (var stringEvent in stringEvents)
            {
                var unityEvent = stringEvent.OnUpdateString;
                var listenerCount = unityEvent.GetPersistentEventCount();
                for (var i = 0; i < listenerCount; i++)
                {
                    unityEvent.SetPersistentListenerState(i, UnityEventCallState.EditorAndRuntime);
                }
                EditorUtility.SetDirty(stringEvent);
            }

            var currentLocale = LocalizationEditorSettings.ActiveLocalizationSettings.GetSelectedLocale();
            if (currentLocale != null)
                typeof(LocalizationSettings).GetMethod("SendLocaleChangedEvents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(LocalizationEditorSettings.ActiveLocalizationSettings, new object[]{currentLocale});
        }

        private static IEnumerable<LocalizeStringEvent> GetAllLocalizeStringEvents()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            return prefabStage != null
                ? prefabStage.prefabContentsRoot.GetComponentsInChildren<LocalizeStringEvent>(true)
                : Object.FindObjectsByType<LocalizeStringEvent>(FindObjectsSortMode.None);
        }

        private class LocaleWrapper
        {
            public readonly Locale Locale;
            public string Name => Locale == null ? "None" : Locale.name;

            public LocaleWrapper(Locale locale)
            {
                Locale = locale;
            }
        }
    }
}
