// Author: Radim Holub
// Created: 15.04.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.XRTools.Utils;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.XRTools.GazeValidation
{
    [DisallowMultipleComponent]
    public class GazeValidationController : MonoBehaviour
    {
        [SerializeField]
        private InteractionPositionSampler _PositionSampler; 
        
        [SerializeField]
        private Toggle _GazeTraceToggle;
        
        [SerializeField]
        private Toggle _DwellSelectToggle;
        
        [SerializeField]
        private Toggle _ReactionChallengeToggle;
        
        [SerializeField]
        [ListDrawerSettings(OnTitleBarGUI = "OnTargetListTitleBarGUI")]
        private List<GazeValidationTarget> _Targets;
        
        private GazeValidationTarget _currentReactionChallengeTarget;
        
        private void OnEnable()
        {
            foreach (var target in _Targets)
            {
                target.SetSelected(false);
            }
            
            if (_GazeTraceToggle != null)
                _GazeTraceToggle.onValueChanged.AddListener(OnGazeTraceToggleChanged);
            
            if (_DwellSelectToggle != null)
                _DwellSelectToggle.onValueChanged.AddListener(OnDwellSelectToggleChanged);
            
            if (_ReactionChallengeToggle != null)                
                _ReactionChallengeToggle.onValueChanged.AddListener(OnReactionChallengeToggleChanged);
            
            if (_PositionSampler && _GazeTraceToggle)
                _PositionSampler.ShowTargetPoints = _GazeTraceToggle.isOn;

            EnableDwellSelect();
        }
        
        private void OnDisable()
        {
            foreach (var target in _Targets)
            {
                if (target == null)
                    continue;
                target.SetSelected(false);
                target.HoverStateChanged -= OnReactionChallengeToggleChanged;
            }
        }
        
        private void OnGazeTraceToggleChanged(bool state)
        {
            if (_PositionSampler)
                _PositionSampler.ShowTargetPoints = state;
        }

        private void OnDwellSelectToggleChanged(bool state)
        {
            if (state) EnableDwellSelect();
        }

        private void OnReactionChallengeToggleChanged(bool state)
        {
            if (state) EnableReactionChallenge();
        }
        
        public void EnableDwellSelect()
        {
            foreach (var target in _Targets)
            {
                if (target == null)
                    continue;
                target.SelectOnHover = true;
                target.HoverStateChanged -= OnReactionChallengeToggleChanged;
            }
        }
        
        public void EnableReactionChallenge()
        {
            foreach (var target in _Targets)
            {
                if (target == null)
                    continue;
                target.SelectOnHover = false;
                target.HoverStateChanged += OnReactionChallengeToggleChanged;
            }
            SelectRandomTarget();
        }

        private void SelectRandomTarget(int exceptedIndex = -1)
        {
            if (_Targets == null || _Targets.Count == 0) 
                return;

            if (exceptedIndex >= 0 && _Targets.Count <= 1) 
                return;

            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, _Targets.Count);
            } while (randomIndex == exceptedIndex);

            _currentReactionChallengeTarget = _Targets[randomIndex];
            _currentReactionChallengeTarget.SetSelected(true);
        }

        private void OnReactionChallengeToggleChanged(GazeValidationTarget target, bool state)
        {
            if (target == _currentReactionChallengeTarget && state)
            {
                target.SetSelected(false);
                SelectRandomTarget(_Targets.IndexOf(target));
            }
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnTargetListTitleBarGUI(InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                _Targets = GetComponentsInChildren<GazeValidationTarget>().ToList();
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}