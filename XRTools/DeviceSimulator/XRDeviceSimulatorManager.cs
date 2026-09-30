// Author: František Holubec
// Created: 15.05.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using UnityEngine;

namespace EDIVE.XRTools.DeviceSimulator
{
    public class XRDeviceSimulatorManager : ALoadableServiceBehaviour<XRDeviceSimulatorManager>
    {
        [SerializeField]
        private GameObject _SimulatorPrefab;
        
        private GameObject _runtimeSimulatorInstance;

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            if (!XRDeviceSimulatorUtils.SimulatorEnabled)
                return UniTask.CompletedTask;

            EnsureSimulatorInstance();
            return UniTask.CompletedTask;
        }

        private void EnsureSimulatorInstance()
        {
            if (_runtimeSimulatorInstance != null)
            {
                if (!_runtimeSimulatorInstance.activeSelf)
                    _runtimeSimulatorInstance.SetActive(true);
                return;
            }
            if (!_SimulatorPrefab)
                return;

            _runtimeSimulatorInstance = Instantiate(_SimulatorPrefab);
            _runtimeSimulatorInstance.name = _SimulatorPrefab.name;
            DontDestroyOnLoad(_runtimeSimulatorInstance);
        }

        private void DisableSimulatorInstance()
        {
            if (_runtimeSimulatorInstance != null && _runtimeSimulatorInstance.activeSelf)
                _runtimeSimulatorInstance.SetActive(false);
        }

        public void SetSimulatorActive(bool value)
        {
            if (value)
                EnsureSimulatorInstance();
            else 
                DisableSimulatorInstance();
        }
    }
}
