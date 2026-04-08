// Author: Michal Petr
// Created: 29.10.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils;
using EDIVE.VisualPresets.Presets;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.Forms.Questions
{
    [Serializable]
    public abstract class AFormQuestion
    {
        public abstract string EditorLabel { get; }
        
        [Required]
        [SerializeField]
        [PropertyOrder(-100)]
        [EnhancedValidate("ValidateID")]
        private string _ID;
        
        [SerializeField]
        private VisualPreset _Visual;
        
        public VisualPreset Visual => _Visual;
        public string ID => _ID;

#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            if (!string.IsNullOrEmpty(_ID) || !property.TryGetParentObject<IEnumerable<AFormQuestion>>(out var parentCollection)) 
                return;
            
            GenerateNewID(property, parentCollection);
        }
        
        [UsedImplicitly]
        public void ValidateID(SelfValidationResult result, InspectorProperty property)
        {
            if (!property.TryGetParentObject(out IEnumerable<AFormQuestion> parentCollection))
                return;
            
            if (parentCollection.Any(q => q != null && q != this && q.ID == _ID))
            {
                result.AddError($"ID '{_ID}' is not unique.").WithFix(() => GenerateNewID(property, parentCollection));
            }
        }
        
        private void GenerateNewID(InspectorProperty property, IEnumerable<AFormQuestion> parentCollection)
        {
            _ID = IdentifierUtility.GeneratePrefixedNumericID(this, parentCollection, q => q.ID, "Q");
            property.MarkSerializationRootDirty();
        }
#endif
    }
}
