// Author: Radim Holub
// Created: 18.06.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.Core;
using EDIVE.DataStructures.VariableFields;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Input.Controls
{
    public class RigHeightModeDropdown : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private TMP_Dropdown _Dropdown;

        [SerializeField]
        [EnhancedTableList]
        private List<HeightModeOption> _Options = new();

        private readonly List<RigHeightMode> _modes = new();

        private void OnEnable()
        {
            if (_Dropdown == null)
                return;

            _modes.Clear();
            _modes.AddRange(EnumUtils.GetValues<RigHeightMode>());

            _Dropdown.ClearOptions();
            _Dropdown.AddOptions(_modes
                .Select(mode => new TMP_Dropdown.OptionData(_Options.TryGetFirst(o => o.Mode == mode, out var option) ? option.Label : mode.ToString()))
                .ToList());

            if (AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
            {
                var index = _modes.IndexOf(controlsManager.CurrentHeightMode);
                if (index >= 0)
                    _Dropdown.SetValueWithoutNotify(index);
            }

            _Dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            _Dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        private void OnDisable()
        {
            if (_Dropdown != null)
                _Dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        }

        private void OnDropdownChanged(int index)
        {
            if (index < 0 || index >= _modes.Count)
                return;

            if (AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
                controlsManager.SetHeightMode(_modes[index]);
        }
        
        [Serializable]
        private class HeightModeOption
        {
            [SerializeField]
            private RigHeightMode _Mode;

            [SerializeField]
            private VariableField<string> _Label = new();

            public RigHeightMode Mode => _Mode;
            public string Label => _Label.Value;
        }
    }
}