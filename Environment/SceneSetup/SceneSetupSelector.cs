// Author: František Holubec
// Created: 28.08.2025

using EDIVE.Core;
using EDIVE.StateHandling.ToggleStates;
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

        // On while this setup is the current one
        [SerializeField]
        private AToggleState _CurrentState;

        private SceneSetupManager _manager;
        
        [Button]
        public void SetDefinition(SceneSetupDefinition definition)
        {
            _Definition = definition;
            if (definition == null)
                return;
            if (_IDText)
                _IDText.text = definition.UniqueID;
            if (_Visual != null)
                _Visual.Apply(definition.Visual);
            RefreshCurrent();
        }
        
        private void OnEnable()
        {
            _Activation?.Register(ChangeSceneSetup);
            if (AppCore.Services.TryGet(out _manager))
                _manager.CurrentSetupChanged += OnCurrentSetupChanged;
            RefreshCurrent();
        }

        private void OnDisable()
        {
            _Activation?.Unregister(ChangeSceneSetup);
            if (_manager != null)
                _manager.CurrentSetupChanged -= OnCurrentSetupChanged;
            _manager = null;
        }

        private void OnCurrentSetupChanged(SceneSetupDefinition definition) => RefreshCurrent();

        private void RefreshCurrent()
        {
            if (_CurrentState != null)
                _CurrentState.SetState(_Definition != null && _manager != null && _manager.CurrentSetup == _Definition);
        }

        private void ChangeSceneSetup()
        {
            if (_Definition == null)
                return;

            if (AppCore.Services.TryGet<SceneSetupManager>(out var sceneContextManager))
                sceneContextManager.SetCurrentSetup(_Definition);
        }
    }
}
