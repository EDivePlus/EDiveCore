// Author: Michal Petr
// Created: 25.05.2026

using System;
using System.Collections.Generic;
using EDIVE.Localization.LocalizeStringModifiers;
using UnityEngine;
using UnityEngine.Localization.Components;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
#endif

namespace EDIVE.Localization
{
    public class EnhancedLocalizeStringEvent : LocalizeStringEvent
    {
        [SerializeReference]
        private List<ILocalizeStringModifier> _Modifiers = new();

        protected override void UpdateString(string value)
        {
            foreach (var modifier in _Modifiers)
            {
                value = modifier.Apply(value);
            }
            base.UpdateString(value);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(EnhancedLocalizeStringEvent))]
    public class EnhancedLocalizeStringEventEditor : NativeWrapperOdinEditor<LocalizeStringEvent>
    {
        private static Type _baseEditorType;
        protected override Type BaseEditorType => _baseEditorType ??= Type.GetType("UnityEditor.Localization.UI.LocalizeStringEventEditor, Unity.Localization.Editor");
    }
#endif
}
