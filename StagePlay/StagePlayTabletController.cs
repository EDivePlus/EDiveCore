// Author: František Holubec
// Created: 05.03.2026

using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EDIVE.StateHandling.MultiStates;
using EDIVE.Utils.Activations;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using EDIVE.XRTools;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.StagePlay
{
    public class StagePlayTabletController : MonoBehaviour
    {
        [SerializeField]
        private StagePlayController _Controller;
        
        [SerializeField]
        private SmoothCameraFollower _CameraFollower;
        
        [SerializeField]
        private Transform _RootTransform;
        
        [SerializeField]
        private float _TweenDuration = 0.3f;
        
        [SerializeField]
        private bool _OpenOnStart = true;
        
        [SerializeReference]
        private IActivation _ToggleActivation;
        
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
        
        [DisableInEditorMode]
        [ShowInInspector]
        public bool IsOpen
        {
            get => _isOpen;
            set => SetOpen(value);
        }
        
        private Tween _animTween;
        private bool _isOpen;

        public enum StagePlayViewState
        {
            Home, 
            Script
        }

        private void Awake()
        {
            if (_ViewState)
                _ViewState.SetState(StagePlayViewState.Home);
        }
        
        private void OnEnable()
        {
            foreach (var selector in _Selectors)
            {
                selector.DefinitionSelected += OnDefinitionSelected;
            }
            _HomeActivation?.RegisterActivationListener(OnHomeActivated);
            _ToggleActivation?.RegisterActivationListener(ToggleTablet);
        }

        private void OnDisable()
        {
            _ToggleActivation?.UnregisterActivationListener(ToggleTablet);
            _HomeActivation?.UnregisterActivationListener(OnHomeActivated);
            foreach (var selector in _Selectors)
            {
                selector.DefinitionSelected -= OnDefinitionSelected;
            }
        }

        private void Start()
        {
            if (_OpenOnStart)
                SetOpen(true);
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
        
        [Button]
        public void ToggleTablet()
        {
            _isOpen = !_isOpen;
            SetOpen(_isOpen);
        }
        
        public void SetOpen(bool open, bool immediate = false)
        {
            _animTween?.Kill();
            _isOpen = open;

            if (_CameraFollower != null && open)
            {
                _CameraFollower.Reposition(immediate);
            }
            
            if (_RootTransform)
            {
                var newScale = open ? Vector3.one : Vector3.zero;
                if (immediate)
                    _RootTransform.localScale = newScale;
                else
                    _animTween = _RootTransform.DOScale(newScale, _TweenDuration).SetEase(Ease.InOutQuad);
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
