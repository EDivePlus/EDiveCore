// Author: František Holubec
// Created: 17.03.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.Actions;
using EDIVE.DataStructures.ToggleableValues;
using EDIVE.EditorUtils;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.BuildSetupData
{
    [Serializable]
    public class SerializedBuildSetupData
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<ToggleableField<string>> _Defines = new();

        [SerializeReference]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false)]
        [CustomValueDrawer(nameof(CustomBuildActionDrawer))]
        [ValueDropdown(nameof(GetAvailableBuildActions), DrawDropdownForListElements = false, ExcludeExistingValuesInList = true)]
        private List<ABuildAction> _Actions = new();

        public IEnumerable<string> Defines => _Defines.ToValueList();
        public IEnumerable<ABuildAction> Actions => _Actions;
        
        private IEnumerable GetAvailableBuildActions() => TypeCacheUtils.GetDerivedClassesOfType<ABuildAction>().Select(a => new ValueDropdownItem<ABuildAction>(a.CallbackName, a));
        
        private const char TOOLTIP_ICON = '\u24d8';
        
        private ABuildAction CustomBuildActionDrawer(ABuildAction value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            if (value == null)
            {
                callNextDrawer(label);
                return null;
            }
            
            var content = string.IsNullOrEmpty(value.Tooltip) ? GUIHelper.TempContent(value.CallbackName) : GUIHelper.TempContent($"{value.CallbackName} {TOOLTIP_ICON}", value.Tooltip);
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
            callNextDrawer(label);
            return value;
        }
    }
}
