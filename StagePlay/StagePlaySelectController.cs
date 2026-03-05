// Author: František Holubec
// Created: 05.03.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.StateHandling.MultiStates;
using EDIVE.Utils.Activations;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.StagePlay
{
    public class StagePlaySelectController : MonoBehaviour
    {
        [SerializeField]
        private StagePlayController _Controller;
        
        [SerializeReference]
        private IActivation _HomeActivation;
        
        [ValidateMultiState(typeof(StagePlayViewState))]
        [SerializeField]
        private AMultiState _ViewState;
        
        [SerializeField]
        [ListDrawerSettings(OnTitleBarGUI = "OnSelectorTitleBarGUI")]
        private List<StagePlaySelector> _Selectors;
        
        [SerializeField]
        private List<StagePlayDefinition> _Definitions;

        public enum StagePlayViewState
        {
            Home, 
            Script
        }

        private void Awake()
        {
            if (_ViewState)
                _ViewState.SetState(StagePlayViewState.Home);
            _HomeActivation?.RegisterActivationListener(OnHomeActivated);
            foreach (var selector in _Selectors)
            {
                selector.DefinitionSelected += OnDefinitionSelected;
            }
        }

        private void OnDestroy()
        {
            _HomeActivation?.UnregisterActivationListener(OnHomeActivated);
            foreach (var selector in _Selectors)
            {
                selector.DefinitionSelected -= OnDefinitionSelected;
            }
        }
        
        private void OnHomeActivated()
        {
            if (_ViewState)
                _ViewState.SetState(StagePlayViewState.Home);
        }
        
        private void OnDefinitionSelected(StagePlayDefinition definition)
        {
            if (_Controller && definition)
            {
                _Controller.SetDefinition(definition);
                
                if (_ViewState)
                    _ViewState.SetState(StagePlayViewState.Script);
            }
        }

#if UNITY_EDITOR
        [Button]
        public void AssignDefinitions()
        {
            for (var index = 0; index < _Selectors.Count; index++)
            {
                if (index >= _Definitions.Count)
                    break;

                if (_Selectors[index] != null && _Definitions[index] != null)
                {
                    _Selectors[index].Definition = _Definitions[index];
                    EditorUtility.SetDirty(_Selectors[index]);
                }
            }
        }

        [UsedImplicitly]
        private void OnSelectorTitleBarGUI(InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                _Selectors = GetComponentsInChildren<StagePlaySelector>(true).ToList();
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
