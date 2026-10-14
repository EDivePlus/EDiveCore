// Author: Michal Petr
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.DataStructures.VariableFields;
using UnityEngine;
using UnityEngine.XR;

namespace EDIVE.ExternalUtils.OpenXR
{
    /// <summary>
    /// Needs the OpenXR Foveated Rendering feature with the SRP Foveation API.
    /// </summary>
    [Serializable]
    public class FoveatedRenderingLoader : ILoadable
    {
        [Tooltip("0 is off, 1 is the strongest.")]
        [SerializeField]
        private VariableField<float> _Level = new(1f);

        public UniTask Load(Action<float> progressCallback)
        {
            var level = _Level.Value;
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var display in displays)
            {
                if (display.running)
                    display.foveatedRenderingLevel = level;
            }
            return UniTask.CompletedTask;
        }
    }
}
