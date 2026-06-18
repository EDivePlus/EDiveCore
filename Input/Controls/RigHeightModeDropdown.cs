// Author: Radim Holub
// Created: 18.06.2026

using System.Linq;
using EDIVE.Core;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Input.Controls
{
    public class RigHeightModeDropdown : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private TMP_Dropdown _Dropdown;

        private void OnEnable()
        {
            if (_Dropdown == null)
                return;

            _Dropdown.ClearOptions();
            _Dropdown.AddOptions(EnumUtils.GetValues<RigHeightMode>()
                .Select(mode => new TMP_Dropdown.OptionData(mode.ToString()))
                .ToList());

            _Dropdown.SetValueWithoutNotify((int) ControlsManager.SavedHeightMode);
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
            var mode = (RigHeightMode) index;
            if (AppCore.Services.TryGet<ControlsManager>(out var controlsManager))
                controlsManager.SetHeightMode(mode);
        }
    }
}
