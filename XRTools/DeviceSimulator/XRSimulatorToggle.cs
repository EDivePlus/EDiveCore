// Author: Radim Holub
// Created: 14.07.2026

using Cysharp.Threading.Tasks;
using EDIVE.Conditions;
using EDIVE.Core.Restart;
using EDIVE.View.ViewTree;
using EDIVE.View.Windows;
using EDIVE.View.Windows.Dialog;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.XRTools.DeviceSimulator
{
    // Simulator change applies through an app restart, confirmed in a dialog
    public class XRSimulatorToggle : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private Toggle _Toggle;

        [Required]
        [SerializeField]
        private WindowDefinition _DialogWindow;

        // Result true restarts
        [Required]
        [SerializeField]
        private DialogSetup _RestartDialog;

        // Used instead while the session condition holds
        [SerializeField]
        private DialogSetup _SessionRestartDialog;

        [SerializeReference]
        private ICondition _SessionCondition;

        private bool _isOpen;

        private void OnEnable()
        {
            if (_Toggle == null)
                return;

            _Toggle.SetIsOnWithoutNotify(XRDeviceSimulatorUtils.SimulatorEnabled);
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
            _Toggle.SetIsOnWithoutNotify(XRDeviceSimulatorUtils.SimulatorEnabled);
            ConfirmAsync(value).Forget();
        }

        private async UniTaskVoid ConfirmAsync(bool value)
        {
            if (_isOpen)
                return;

            var screen = this.FindInViewTree<ScreenRoot>();
            if (screen == null)
            {
                Debug.LogWarning($"[XRSimulatorToggle] {name} is not under a ScreenRoot.", this);
                return;
            }

            var inSession = _SessionRestartDialog != null && _SessionCondition != null && _SessionCondition.Evaluate();
            _isOpen = true;
            var result = await screen.Open(_DialogWindow, inSession ? _SessionRestartDialog : _RestartDialog).Closed;
            _isOpen = false;

            if (result is not true)
                return;

            XRDeviceSimulatorUtils.SimulatorEnabled = value;
            AppRestartUtility.Restart();
        }
    }
}
