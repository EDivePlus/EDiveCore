// Author: František Holubec
// Created: 31.03.2025

#if UNITY_EDITOR
using EDIVE.External.ToolbarExtensions;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.MirrorNetworking
{
    public static class NetworkToolbarExtensions
    {
        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            ToolbarExtender.AddToLeftPlayButtons(NetworkToolbarGUI, -10);
        }

        private static void NetworkToolbarGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            EditorGUI.BeginChangeCheck();
            var newMode = (NetworkRuntimeMode) SirenixEditorFields.EnumDropdown(null, NetworkUtils.EditorRuntimeMode, ToolbarStyles.ToolbarDropdown, GUILayout.Width(70));
            if (EditorGUI.EndChangeCheck())
            {
                NetworkUtils.EditorRuntimeMode = newMode;
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(2);
        }
    }
}
#endif
