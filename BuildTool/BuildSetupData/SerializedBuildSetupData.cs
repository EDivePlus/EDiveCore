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
    public class SerializedBuildSetupData : IBuildSetupData
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<ToggleableField<string>> _Defines = new();

        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = false)]
        [HideReferenceObjectPicker]
        [CustomValueDrawer(nameof(CustomBuildActionDrawer))]
        [ValueDropdown(nameof(GetAvailableBuildActions), DrawDropdownForListElements = false, ExcludeExistingValuesInList = true)]
        private List<IBuildAction> _Actions = new();

        public IEnumerable<string> Defines => _Defines.ToValueList();
        public IEnumerable<IBuildAction> Actions => _Actions;
        
        private IEnumerable GetAvailableBuildActions() => 
            TypeCacheUtils.GetDerivedClassesOfType<IBuildAction>().Select(a => new ValueDropdownItem<IBuildAction>(a.Label, a));
        
        private const char TOOLTIP_ICON = '\u24d8';
        
        private IBuildAction CustomBuildActionDrawer(IBuildAction value, Func<GUIContent, bool> callNextDrawer)
        {
            var content = string.IsNullOrEmpty(value.Tooltip) ? GUIHelper.TempContent(value.Label) : GUIHelper.TempContent($"{value.Label} {TOOLTIP_ICON}", value.Tooltip); 
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
            
            callNextDrawer(null);
            return value;
        }
    }
}
