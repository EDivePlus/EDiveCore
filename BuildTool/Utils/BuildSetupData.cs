// Author: František Holubec
// Created: 17.03.2025

using System;
using System.Collections;
using System.Collections.Generic;
using EDIVE.BuildTool.Actions;
using EDIVE.EditorUtils;
using EDIVE.Utils.ToggleableValues;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.BuildTool.Utils
{
    [Serializable]
    public class BuildSetupData
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<ToggleableField<string>> _Defines = new();

        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = false)]
        [HideReferenceObjectPicker]
        [ValueDropdown(nameof(GetAvailableBuildActions), DrawDropdownForListElements = false, ExcludeExistingValuesInList = true)]
        private List<ABuildAction> _Actions = new();

        public IEnumerable<string> Defines => _Defines.ToValueList();
        public IEnumerable<ABuildAction> Actions => _Actions;

        private IEnumerable GetAvailableBuildActions() => TypeCacheUtils.GetDerivedClassesOfType<ABuildAction>();
    }
}
