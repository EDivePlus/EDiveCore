// Author: František Holubec
// Created: 09.10.2026

using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.ToggleStates;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.ViewTree.StateHandling
{
    // Applies enabled presets while active, disabled presets otherwise
    public class ToggleStateViewAdapter : AToggleViewAdapter
    {
        [SerializeField]
        [HideReferenceObjectPicker]
        [EnhancedValidate("ValidateRecords")]
        private List<ToggleStateRecord> _Records = new();

        public List<ToggleStateRecord> Records => _Records;

        protected override void SetActive(bool active)
        {
            foreach (var record in _Records)
                record?.SetState(active);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateRecords(SelfValidationResult result)
        {
            foreach (var record in _Records)
            {
                if (record?.Target is GameObject target && transform.IsChildOf(target.transform))
                    result.AddWarning($"[View Tree] Record targets this object or its parent '{target.name}', an Active preset would stop the node.");
            }
        }
#endif
    }
}
