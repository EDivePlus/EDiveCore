// Author: Michal Petr
// Created: 29.10.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.AssetTranslation;
using EDIVE.EditorUtils;
using EDIVE.Forms.Questions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Forms
{
    public class FormDefinition : AUniqueDefinition
    {
        [SerializeReference]
        [HideReferenceObjectPicker]
        [EnhancedValueDropdown("GetQuestionsDropdown", DrawDropdownForListElements = false)]
        [CustomValueDrawer("CustomQuestionDrawer")]
        private List<AFormQuestion> _Questions = new();
        
        [SerializeField]
        private VisualPreset _Visual;
        
        public List<AFormQuestion> Questions => _Questions;
        public VisualPreset Visual => _Visual;
        
#if UNITY_EDITOR
        private IEnumerable GetQuestionsDropdown()
        {
            return TypeCacheUtils.GetAssignableClassesOfType<AFormQuestion>().Select(t => new ValueDropdownItem(t.EditorLabel, t));
        }
        
        private AFormQuestion CustomQuestionDrawer(AFormQuestion value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            if (value != null) 
                GUILayout.Label(value.EditorLabel, EditorStyles.boldLabel);
            callNextDrawer(label);
            return value;
        }
#endif
    }
}
