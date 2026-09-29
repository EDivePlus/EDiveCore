// Author: František Holubec
// Created: 15.05.2025
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace EDIVE.XRTools.DeviceSimulator
{
    public static class XRDeviceSimulatorUtils
    {
        private const string RUNTIME_ENABLED_PREF_KEY = "XRSimulator_RuntimeEnabled";

        private static GameObject _SimulatorInstance;

        public static bool SimulatorEnabled => RuntimeSimulatorEnabled ||
                                               (XRDeviceSimulatorSettings.Instance.automaticallyInstantiateSimulatorPrefab &&
                                                (!XRDeviceSimulatorSettings.Instance.automaticallyInstantiateInEditorOnly || Application.isEditor));

        public static bool RuntimeSimulatorEnabled
        {
            get => PlayerPrefs.GetInt(RUNTIME_ENABLED_PREF_KEY, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(RUNTIME_ENABLED_PREF_KEY, value ? 1 : 0);
                if (value)
                    EnsureSimulatorInstance();
                else
                    DisableSimulatorInstance();
            }
        }

        public static bool AutoInstantiateSimulator
        {
            get => XRDeviceSimulatorSettings.Instance.automaticallyInstantiateSimulatorPrefab;
            set => XRDeviceSimulatorSettings.Instance.automaticallyInstantiateSimulatorPrefab = value;
        }

        // This method is used to fix the issue with the XRDeviceSimulator not being instantiated
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad), Preserve]
        public static void Initialize()
        {
            if (!SimulatorEnabled)
                return;

            EnsureSimulatorInstance();
        }

        private static void EnsureSimulatorInstance()
        {
            if (_SimulatorInstance == null)
                _SimulatorInstance = ResolveExistingInstance();

            if (_SimulatorInstance != null)
            {
                if (!_SimulatorInstance.activeSelf)
                    _SimulatorInstance.SetActive(true);
                return;
            }

            var simulatorPrefab = XRDeviceSimulatorSettings.Instance.simulatorPrefab;
            if (!simulatorPrefab)
                return;

            _SimulatorInstance = Object.Instantiate(simulatorPrefab);
            _SimulatorInstance.name = simulatorPrefab.name;
            Object.DontDestroyOnLoad(_SimulatorInstance);
        }

        private static void DisableSimulatorInstance()
        {
            if (_SimulatorInstance == null)
                _SimulatorInstance = ResolveExistingInstance();

            if (_SimulatorInstance != null && _SimulatorInstance.activeSelf)
                _SimulatorInstance.SetActive(false);
        }

        private static GameObject ResolveExistingInstance()
        {
            if (XRInteractionSimulator.instance)
                return XRInteractionSimulator.instance.gameObject;

            if (XRDeviceSimulator.instance)
                return XRDeviceSimulator.instance.gameObject;

            return null;
        }
    }
}
