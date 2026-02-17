// Author: František Holubec
// Created: 17.02.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.VisualPresets.StateHandling
{
    public class MultiStateVisualID : ABaseVisualID
    {
        [EnhancedValidate("ValidateStates")]
        [ListDrawerSettings(ShowFoldout = false)]
        [SerializeField]
        private List<string> _AvailableStates = new();

        public List<string> AvailableStates => _AvailableStates;
        
        public bool HasState(string state)
        {
            return AvailableStates != null && AvailableStates.Contains(state);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateStates(List<string> value, SelfValidationResult result, InspectorProperty property)
        {
            if (value == null)
                return;

            // Check for null, empty or whitespace strings
            var invalidStates = value.Where(string.IsNullOrWhiteSpace);
            if (invalidStates.Any())
            {
                result.AddError("States cannot be null, empty or whitespace")
                    .WithFix(() =>
                    {
                        value.RemoveAll(string.IsNullOrWhiteSpace);
                        property.MarkSerializationRootDirty();
                    });
            }

            // Check for duplicate states
            var duplicateStates = value.Where(s => !string.IsNullOrWhiteSpace(s))
                .GroupBy(s => s)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateStates.Any())
            {
                result.AddError($"Duplicate states found: {string.Join(", ", duplicateStates)}").WithFix(() =>
                {
                    var distinctStates = value.Distinct().ToList();
                    value.Clear();
                    value.AddRange(distinctStates);
                    property.MarkSerializationRootDirty();
                });
            }
        }
#endif
    }
}
