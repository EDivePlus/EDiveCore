#if UNITY_6000_3_OR_NEWER
#define UNITY_6_TOOLBAR
#endif

using System.Collections.Generic;
using System.Linq;
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
using Object = UnityEngine.Object;

#if UNITY_6_TOOLBAR
using EDIVE.EditorUtils;
using UnityEditor.Toolbars;
#else
using EDIVE.External.ToolbarExtensions;
#endif

namespace EDIVE.Localization.Editor
{
    public static class LocalizationToolbarExtension
    {
#if UNITY_6_TOOLBAR
        [MainToolbarElement("EDive/Locale Selector", defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreatePlayRootSceneButton()
        {
            return MainToolbarUtility.CreateElement(() =>
            {
                var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                var dropdown = new EditorToolbarDropdown
                {
                    icon = FontAwesomeEditorIcons.LanguageSolid.Raw,
                    tooltip = "Language Selector"
                };
                dropdown.AddToClassList("unity-editor-toolbar-element");
                dropdown.clicked += () =>
                {
                    if (!settings) 
                        return;
                    
                    var locales = settings.GetAvailableLocales().Locales
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
                            settings.SetSelectedLocale(selected.Locale);
                            RefreshAll();
                        }
                    };
                    selector.ShowInPopup(dropdown.worldBound.MinWidth(200));
                };
                
                settings.OnSelectedLocaleChanged += UpdateLabel;
                UpdateLabel(settings.GetSelectedLocale());
                return dropdown;

                void UpdateLabel(Locale locale)
                {
                    dropdown.text = locale == null ? "--" : locale.Identifier.Code.ToUpperInvariant();
                }
            });
        }
#else  
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
#endif
        
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
