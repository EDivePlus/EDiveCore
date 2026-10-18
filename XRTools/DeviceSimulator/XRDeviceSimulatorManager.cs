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

        // Simulator outlives scenes, gone with the manager on app restart
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_runtimeSimulatorInstance != null)
                Destroy(_runtimeSimulatorInstance);
        }

        private void EnsureSimulatorInstance()
        {
            if (_runtimeSimulatorInstance != null || !_SimulatorPrefab)
                return;

            _runtimeSimulatorInstance = Instantiate(_SimulatorPrefab);
            _runtimeSimulatorInstance.name = _SimulatorPrefab.name;
            DontDestroyOnLoad(_runtimeSimulatorInstance);
        }
    }
}
