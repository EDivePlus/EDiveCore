// Author: František Holubec
// Created: 15.05.2025

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace EDIVE.XRTools.DeviceSimulator
{
    public static class XRDeviceSimulatorUtils
    {
        public static bool SimulatorEnabled => XRDeviceSimulatorSettings.Instance.automaticallyInstantiateSimulatorPrefab &&
                                               (!XRDeviceSimulatorSettings.Instance.automaticallyInstantiateInEditorOnly || Application.isEditor);

        // This method is used to fix the issue with the XRDeviceSimulator not being instantiated
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad), Preserve]
        public static void Initialize()
        {
            if (!SimulatorEnabled)
                return;

            if (XRInteractionSimulator.instance || XRDeviceSimulator.instance)
                return;

            var simulatorPrefab = XRDeviceSimulatorSettings.Instance.simulatorPrefab;
            if (!simulatorPrefab)
                return;

            var simulatorInstance = Object.Instantiate(simulatorPrefab);
            simulatorInstance.name = simulatorPrefab.name;
            Object.DontDestroyOnLoad(simulatorInstance);
        }
    }
}
