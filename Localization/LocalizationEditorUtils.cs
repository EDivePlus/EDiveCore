// Author: Michal Petr
// Created: 25.05.2026

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace EDIVE.Localization
{
    public static class LocalizationEditorUtils
    {
        [MenuItem("CONTEXT/LocalizeStringEvent/Convert to Enhanced", false, 10000)]
        public static void ConvertToEnhanced(MenuCommand command)
        {
            if (command.context is not LocalizeStringEvent behaviour)
                return;

            var newBehaviour = behaviour.ChangeScriptType<EnhancedLocalizeStringEvent>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(behaviour), newBehaviour);
            EditorUtility.SetDirty(newBehaviour);
        }
    }
}

#endif