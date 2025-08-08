// Author: František Holubec
// Created: 31.03.2025

#if UNITY_EDITOR
using EDIVE.External.ToolbarExtensions;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    public static class NetworkToolbarExtensions
    {
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
            var newMode = EnumSelector<NetworkRuntimeMode>.DrawEnumField(null, NetworkUtils.EditorRuntimeMode);
            if (EditorGUI.EndChangeCheck())
            {
                NetworkUtils.EditorRuntimeMode = newMode;
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(1);
        }
    }
}
#endif
