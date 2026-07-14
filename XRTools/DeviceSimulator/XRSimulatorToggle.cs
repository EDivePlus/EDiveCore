// Author: Radim Holub
// Created: 14.07.2026

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.XRTools.DeviceSimulator
{
    public class XRSimulatorToggle : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private Toggle _Toggle;

        private void OnEnable()
        {
            if (_Toggle == null)
                return;

            _Toggle.SetIsOnWithoutNotify(XRDeviceSimulatorUtils.RuntimeSimulatorEnabled);
            _Toggle.onValueChanged.RemoveListener(OnToggleChanged);
            _Toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        private void OnDisable()
        {
            if (_Toggle != null)
                _Toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        private void OnToggleChanged(bool value)
        {
            XRDeviceSimulatorUtils.RuntimeSimulatorEnabled = value;
        }
    }
}
