// Author: František Holubec
// Created: 28.08.2025

using EDIVE.Core;
using EDIVE.Utils.Activations;
using EDIVE.VisualPresets.Switchers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupSelector : MonoBehaviour
    {
        [SerializeField]
        private SceneSetupDefinition _Definition;

        [SerializeReference]
        private IActivation _Activation;

        [SerializeField]
        private TMP_Text _IDText;

        [SerializeField]
        private VisualSwitcher _Visual;
        
        [Button]
        public void SetDefinition(SceneSetupDefinition definition)
        {
            _Definition = definition;
            _IDText.text = definition.UniqueID;
            _Visual?.Apply(definition.Visual);
        }
        
        private void OnEnable()
        {
            _Activation?.RegisterActivationListener(ChangeSceneSetup);
        }

        private void OnDisable()
        {
            _Activation?.UnregisterActivationListener(ChangeSceneSetup);
        }

        private void ChangeSceneSetup()
        {
            if (_Definition == null)
                return;

            if (AppCore.Services.TryGet<SceneSetupManager>(out var sceneContextManager))
                sceneContextManager.SetCurrentContext(_Definition);
        }
    }
}
