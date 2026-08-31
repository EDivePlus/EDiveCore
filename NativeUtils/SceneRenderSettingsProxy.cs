// Author: František Holubec
// Created: 31.08.2026

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    [DisallowMultipleComponent]
    public class SceneRenderSettingsProxy : MonoBehaviour
    {
        [ShowInInspector]
        public Material Skybox
        {
            get => RenderSettings.skybox;
            set
            {
                RenderSettings.skybox = value;
                DynamicGI.UpdateEnvironment();
            }
        }
    }
}
