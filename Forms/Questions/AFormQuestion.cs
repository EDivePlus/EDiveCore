// Author: Michal Petr
// Created: 29.10.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using ZLinq;


namespace EDIVE.Forms.Questions
{
    [Serializable]
    public abstract class AFormQuestion
    {
        public abstract string EditorLabel { get; }
        
        [Required]
        [SerializeField]
        [PropertyOrder(-100)]
        [EnhancedValidate("ValidateUniqueID")]
        private string _UniqueID;
        
        [SerializeField]
        private VisualPreset _Visual;
        
        public VisualPreset Visual => _Visual;
        public string UniqueID => _UniqueID;

#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            if (string.IsNullOrEmpty(_UniqueID) && property.TryGetParentObject<IEnumerable<AFormQuestion>>(out var parentCollection))
            {
                var highestID = parentCollection.AsValueEnumerable()
                    .Where(q => q != null && q != this && q.UniqueID.StartsWith("Q") && int.TryParse(q.UniqueID[1..], out _))
                    .Select(q => int.Parse(q.UniqueID[1..]))
                    .Prepend(0)
                    .Max();
                _UniqueID = $"Q{highestID + 1:D2}";
                property.MarkSerializationRootDirty();
            }
        }

        [UsedImplicitly]
        public void ValidateUniqueID(SelfValidationResult result, InspectorProperty property)
        {
            if (!property.TryGetParentObject(out FormDefinition questionSet))
                return;
            
            if (questionSet.Questions.Any(q => q != null && q != this && q.UniqueID == _UniqueID))
            {
                result.AddError($"UniqueID '{_UniqueID}' is not unique.");
            }
        }
#endif
    }
}
