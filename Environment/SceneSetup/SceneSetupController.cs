// Author: František Holubec
// Created: 05.09.2025

using EDIVE.Core;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupController : MonoBehaviour
    {
        [SerializeField]
        private VisualSwitcher _Visual;

        public void ApplyDefinition(SceneSetupDefinition definition)
        {
            _Visual?.Apply(definition.Visual);
        }
        
        private void OnEnable()
        {
            if (AppCore.Services.TryGet<SceneSetupManager>(out var sceneSetupManager))
            {
                sceneSetupManager.RegisterSceneController(this);
            }
        }

        private void OnDisable()
        {
            if (AppCore.Services.TryGet<SceneSetupManager>(out var sceneSetupManager))
            {
                sceneSetupManager.UnregisterSceneController(this);
            }
        }
    }
}
