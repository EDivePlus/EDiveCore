// Author: Michal Petr
// Created: 30.09.2026

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace EDIVE.XRTools.DeviceSimulator
{
    public static class XRDeviceSimulatorUtils
    {
        private const string RUNTIME_ENABLED_PREF_KEY = "XRSimulator_RuntimeEnabled";
        
        public static bool SimulatorEnabled
        {
            get => PlayerPrefs.GetInt(RUNTIME_ENABLED_PREF_KEY, 0) != 0;
            // Applies on next app start
            set => PlayerPrefs.SetInt(RUNTIME_ENABLED_PREF_KEY, value ? 1 : 0);
        }
        
        // We use our own simulator
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            XRDeviceSimulatorSettings.Instance.automaticallyInstantiateSimulatorPrefab = false;
        }
    }
}
