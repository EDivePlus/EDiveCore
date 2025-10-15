// Author: František Holubec
// Created: 17.03.2025

using System;
using System.Collections;
using System.Collections.Generic;
using EDIVE.BuildTool.Actions;
using EDIVE.DataStructures.ToggleableValues;
using EDIVE.EditorUtils;
using Sirenix.OdinInspector;
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
        [ValueDropdown(nameof(GetAvailableBuildActions), DrawDropdownForListElements = false, ExcludeExistingValuesInList = true)]
        private List<IBuildAction> _Actions = new();

        public IEnumerable<string> Defines => _Defines.ToValueList();
        public IEnumerable<IBuildAction> Actions => _Actions;
        
        private IEnumerable GetAvailableBuildActions() => TypeCacheUtils.GetDerivedClassesOfType<IBuildAction>();
    }
}
