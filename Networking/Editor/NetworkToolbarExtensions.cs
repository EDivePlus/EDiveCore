// Author: František Holubec
// Created: 31.03.2025

#if UNITY_6000_3_OR_NEWER
#define UNITY_6_TOOLBAR
#endif

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;

#if UNITY_6_TOOLBAR
using System.Linq;
using Sirenix.Utilities;
using UnityEditor.Toolbars;
using EDIVE.OdinExtensions;
using EDIVE.EditorUtils;
#else
using System;
using UnityEngine;
using EDIVE.External.ToolbarExtensions;
#endif

namespace EDIVE.Networking.Utils
{
    public static class NetworkToolbarExtensions
    {
#if UNITY_6_TOOLBAR
        [MainToolbarElement("EDive/Network State", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -5)]
        public static MainToolbarElement CreatePlayRootSceneButton()
        {
            return MainToolbarUtility.CreateElement(() =>
            {
                var dropdown = new EditorToolbarDropdown
                {
                    icon = FontAwesomeEditorIcons.GlobeSolid.Raw,
                    tooltip = "Network State"
                };
                dropdown.AddToClassList("unity-editor-toolbar-element");
                dropdown.style.marginLeft = 10;
                dropdown.style.marginRight = 10;
                dropdown.clicked += () =>
                {
                    var selector = new EnumSelector<NetworkRuntimeMode>();
                    selector.EnableSingleClickToSelect();
                    selector.SetSelection(NetworkUtils.EditorRuntimeMode);
                    selector.SelectionConfirmed += selection =>
                    {
                        if (selection.Any())
                        {
                            NetworkUtils.EditorRuntimeMode = selection.First();
                            UpdateLabel();
                        }
                    };
                    selector.ShowInPopup(dropdown.worldBound.MinWidth(200));
                };
                
                dropdown.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                EditorApplication.playModeStateChanged += _ =>
                {
                    dropdown.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                };
                UpdateLabel();
                return dropdown;

                void UpdateLabel()
                {
                    dropdown.text = NetworkUtils.EditorRuntimeMode.ToString();
                }
            });
        }
#else
        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            ToolbarExtender.AddToLeftToolbar(NetworkToolbarGUI, 990);
        }

        private static void NetworkToolbarGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            EditorGUILayout.BeginVertical(GUILayout.Width(70));
            EditorGUI.BeginChangeCheck();
            var newMode = EnumSelector<NetworkRuntimeMode>.DrawEnumField(null, NetworkUtils.EditorRuntimeMode, ToolbarStyles.ToolbarDropdown);
            if (EditorGUI.EndChangeCheck())
            {
                NetworkUtils.EditorRuntimeMode = newMode;
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(1);
        }
#endif
    }
}
#endif
