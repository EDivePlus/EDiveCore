// Author: František Holubec
// Created: 21.10.2025

using System;
using EDIVE.Conditions;
using EDIVE.XRTools.DeviceSimulator;
using UnityEngine;
using UnityEngine.XR;

namespace EDIVE.XRTools.Conditions
{
    [Serializable]
    public class XRCondition : ABoolCondition
    {
        [SerializeField]
        private bool _IncludeSimulator;

        protected override bool GetValue()
        {
            return XRSettings.enabled || XRSettings.isDeviceActive || (_IncludeSimulator && XRDeviceSimulatorUtils.SimulatorEnabled);
        }
    }
}
