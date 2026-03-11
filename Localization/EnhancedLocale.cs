// Author: Michal Petr
// Created: 11.03.2026

using System;
using EDIVE.VisualPresets.Presets;
using UnityEngine;
using UnityEngine.Localization;

#if UNITY_EDITOR
using UnityEditor;
using EDIVE.OdinExtensions.Editor;
#endif

namespace EDIVE.Localization
{
    public class EnhancedLocale : Locale
    {
        [SerializeField]
        private VisualPreset _VisualPreset = new();
        
        public VisualPreset VisualPreset => _VisualPreset;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EnhancedLocale))]
    [CanEditMultipleObjects]
    public class EnhancedLocaleEditor : NativeWrapperOdinEditor<Locale>
    {
        private static Type _baseEditorType;
        
        protected override Type BaseEditorType => _baseEditorType ??= Type.GetType("UnityEditor.Localization.UI.LocaleEditor, Unity.Localization.Editor");
    }
#endif
}
