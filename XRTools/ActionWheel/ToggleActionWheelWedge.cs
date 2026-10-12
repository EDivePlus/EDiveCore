// Author: Michal Petr
// Created: 22.09.2026

using System;
using System.Collections.Generic;
using EDIVE.Conditions;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Actions;
using UnityEngine;

namespace EDIVE.XRTools.ActionWheel
{
    public class ToggleActionWheelWedge : AActionWheelWedge
    {
        [SerializeReference]
        private List<IAction> _OnActions = new();
        
        [SerializeReference]
        private List<IAction> _OffActions = new();

        [SerializeField]
        private AToggleState _StateVisual;

        [SerializeReference]
        private ABoolCondition _StateCondition;

        private IDisposable _stateObserver;
        
        private bool State => _StateCondition?.Evaluate() ?? false;

        private void OnEnable()
        {
            _stateObserver = _StateCondition.ObserveDisposable(RefreshState);
            
            RefreshState();
        }

        private void OnDisable()
        {
            _stateObserver?.Dispose();
            _stateObserver = null;
        }

        public override void OnShow()
        {
            base.OnShow();
            RefreshState();
        }

        public override void ExecuteActions()
        {
            if (State)
                _OnActions.ForEach(action => action.Execute());
            else
                _OffActions.ForEach(action => action.Execute());

            RefreshState();
        }

        private void RefreshState()
        {
            if (_StateVisual)
                _StateVisual.SetState(State);
        }
    }
}
