// Author: František Holubec
// Created: 14.10.2026

using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.Windows.Buttons
{
    // Apps a screen offers, all of them for the library, the pinned ones for the dock
    [CreateAssetMenu(menuName = "EDIVE/View/Window Catalog", fileName = "WindowCatalog")]
    public class WindowCatalog : ScriptableObject
    {
        [SerializeField]
        [EnhancedValidate("ValidateWindows")]
        private List<WindowDefinition> _Windows = new();

        [Tooltip("Always in the dock, in this order")]
        [SerializeField]
        [EnhancedValidate("ValidatePinned")]
        private List<WindowDefinition> _Pinned = new();

        public IReadOnlyList<WindowDefinition> Windows => _Windows;
        public IReadOnlyList<WindowDefinition> Pinned => _Pinned;

        public bool Contains(WindowDefinition definition) => _Windows.Contains(definition);

#if UNITY_EDITOR
        private void ValidateWindows(SelfValidationResult result)
        {
            var seen = new HashSet<WindowDefinition>();
            foreach (var window in _Windows)
            {
                if (window == null)
                    result.AddError("Empty window entry.");
                else if (!seen.Add(window))
                    result.AddError($"{window.name} is listed twice.");
            }
        }

        private void ValidatePinned(SelfValidationResult result)
        {
            foreach (var window in _Pinned)
            {
                if (window != null && !_Windows.Contains(window))
                    result.AddError($"Pinned {window.name} is not in the window list.");
            }
        }
#endif
    }
}
