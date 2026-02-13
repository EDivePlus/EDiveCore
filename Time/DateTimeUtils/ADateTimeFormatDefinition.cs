// Author: František Holubec
// Created: 13.02.2026

using System;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace EDIVE.Time.DateTimeUtils
{
    public abstract class ADateTimeFormatDefinition : ScriptableObject
    {
        public abstract string Format(DateTime dateTime);
        
#if UNITY_EDITOR
        [PropertyOrder(1000)]
        [PropertySpace]
        [ShowInInspector]
        [CustomValueDrawer(nameof(PreviewValueDrawer))]
        private DateTime _formatPreview = DateTime.Now;
        
        private void PreviewValueDrawer(DateTime value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            SirenixEditorGUI.BeginVerticalPropertyLayout(label);
            callNextDrawer(null);
            GUILayout.Label(Format(value));
            SirenixEditorGUI.EndVerticalPropertyLayout();
        }
#endif
    }
}
