// Author: František Holubec
// Created: 29.10.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.View.Windows.Buttons;
using EDIVE.VisualPresets.Switchers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Networking.ServiceHub
{
    // Covers a window while a requirement is missing, the first missing one picks the look
    public class RequirementOverlay : MonoBehaviour
    {
        [SerializeReference]
        private List<ARequirement> _Requirements = new();

        [SerializeField]
        [Required]
        private AToggleState _ShownState;

        [SerializeField]
        private VisualSwitcher _Switcher = new();

        [SerializeField]
        private WindowButton _FixButton;

        [SerializeField]
        [Tooltip("On while the missing requirement has a window to fix it")]
        private AToggleState _FixButtonState;

        private void OnEnable()
        {
            foreach (var requirement in _Requirements.Where(r => r != null))
                requirement.Bind(Refresh);
            Refresh();
        }

        private void OnDisable()
        {
            foreach (var requirement in _Requirements.Where(r => r != null))
                requirement.Unbind();
        }

        private void Refresh()
        {
            var missing = _Requirements.FirstOrDefault(r => r != null && !r.IsMet);
            _ShownState.SetState(missing != null);
            if (missing == null)
                return;

            if (missing.Look)
                _Switcher.Apply(missing.Look.Preset);
            if (_FixButton && missing.FixWindow)
                _FixButton.Setup(missing.FixWindow);
            if (_FixButtonState)
                _FixButtonState.SetState(missing.FixWindow);
        }
    }
}
