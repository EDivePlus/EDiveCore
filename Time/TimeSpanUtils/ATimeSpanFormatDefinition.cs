// Author: František Holubec
// Created: 22.07.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Time.TimeSpanUtils
{
    public abstract class ATimeSpanFormatDefinition : ScriptableObject
    {
        public abstract string Format(TimeSpan timeSpan);
        
#if UNITY_EDITOR
        [PropertyOrder(1000)]
        [PropertySpace]
        [ShowInInspector]
        [CustomValueDrawer(nameof(PreviewValueDrawer))]
        private TimeSpan _formatPreview = DateTime.Now.TimeOfDay;
        
        private void PreviewValueDrawer(TimeSpan value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            SirenixEditorGUI.BeginVerticalPropertyLayout(label);
            callNextDrawer(null);
            GUILayout.Label(Format(value));
            SirenixEditorGUI.EndVerticalPropertyLayout();
        }
#endif
    }
}
