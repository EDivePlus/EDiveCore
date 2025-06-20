// Author: František Holubec
// Created: 20.06.2025

using EDIVE.XRTools.DeviceSimulator;

namespace EDIVE.XRTools
{
    public static class XRUtils
    {
        public static bool XREnabled => UnityEngine.XR.XRSettings.enabled || XRDeviceSimulatorUtils.SimulatorEnabled;
    }
}
