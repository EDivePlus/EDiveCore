// Author: František Holubec
// Created: 20.10.2025

using System;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Conditions
{
    [EnhancedTypeSelector(true, 1)]
    public interface ICondition
    {
        bool Evaluate();
        
        event Action StateChanged;
        void InitializeObserving();
        void TerminateObserving();
    }

#if UNITY_EDITOR
    [DrawerPriority(5)]
    public class ConditionDrawer : OdinValueDrawer<ICondition>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (ValueEntry.SmartValue == null)
            {
                CallNextDrawer(label);
                return;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            CallNextDrawer(label);
            EditorGUILayout.EndVertical();
            var conditionValue = ValueEntry.SmartValue.Evaluate();
            var conditionText = conditionValue ? "Condition is true" : "Condition is false";
            var conditionIcon = conditionValue ? FontAwesomeEditorIcons.CircleCheckSolid.Raw : FontAwesomeEditorIcons.CircleXmarkSolid.Raw;
            var conditionColor = conditionValue ? Color.green : Color.red;
            GUIHelper.PushColor(conditionColor);
            GUILayout.Label(GUIHelper.TempContent(conditionIcon, conditionText), GUILayout.Width(18), GUILayout.Height(18));
            GUIHelper.PopColor();
            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}
