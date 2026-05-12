// Author: Michal Petr
// Created: 11.05.2026

using System.Collections.Generic;
using EDIVE.Core;
using EDIVE.ServiceHub.RemoteContent.Handlers;
using EDIVE.ServiceHub.RemoteContent.UI;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent
{
    public class FocusedHandlerController : MonoBehaviour
    {
        [SerializeField]
        private List<RemoteContentInfoDisplay> _Displays;

        [SerializeField]
        private AToggleState _AnyHandlerFocusedState;
        
        [SerializeReference]
        private IActivation _DeleteActivation;

        private ARemoteContentHandler _currentHandle;
        
        private void OnEnable()
        {
            if (AppCore.Services.TryGet<RemoteContentManager>(out var manager))
            {
                manager.FocusedHandlerChanged += OnFocusedHandlerChanged;
                OnFocusedHandlerChanged(manager.FocusedHandler);
            }
            
            _DeleteActivation?.RegisterActivationListener(OnDeleteActivated);
        }

        private void OnDisable()
        {
            if (AppCore.Services.TryGet<RemoteContentManager>(out var manager))
            {
                manager.FocusedHandlerChanged -= OnFocusedHandlerChanged;
                _AnyHandlerFocusedState.SetState(false);
            }
            
            _DeleteActivation?.UnregisterActivationListener(OnDeleteActivated);
        }

        private void OnFocusedHandlerChanged(ARemoteContentHandler selected)
        {
            _currentHandle = selected;
            _AnyHandlerFocusedState.SetState(_currentHandle != null);

            if (_currentHandle == null)
                return;
            
            UpdateAppliers();
        }
        
        private void UpdateAppliers()
        {
            var contentInfo = _currentHandle?.ContentInfo;
            foreach (var applier in _Displays)
            {
                applier.ApplyContentInfo(contentInfo);
            }
        }

        private void OnDeleteActivated()
        {
            if (AppCore.Services.TryGet<RemoteContentManager>(out var manager))
            {
                manager.DespawnHandler(_currentHandle);
            }
        }
    }
}
