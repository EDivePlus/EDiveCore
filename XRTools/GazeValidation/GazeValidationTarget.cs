// Author: František Holubec
// Created: 17.04.2026

using System;
using EDIVE.StateHandling.ToggleStates;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EDIVE.XRTools.GazeValidation
{
    internal class GazeValidationTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private AToggleState _SelectedState;
        
        public event Action<GazeValidationTarget, bool> HoverStateChanged;
        
        private bool _selectOnHover;
        public bool SelectOnHover
        {
            get => _selectOnHover;
            set
            {
                _selectOnHover = value;
                _SelectedState.SetState(_selectOnHover && _hoverCount > 0);
            }
        }
        
        private int _hoverCount;
        
        private void OnEnable()
        {
            _SelectedState.SetState(false);
        }
        
        private void OnDisable()
        {
            _SelectedState.SetState(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hoverCount++;
            if (SelectOnHover)
                _SelectedState.SetState(true);
            HoverStateChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hoverCount--;
            if (_hoverCount <= 0)
            {
                _hoverCount = 0;
                _SelectedState.SetState(false);
                HoverStateChanged?.Invoke(this, false);
            }
        }
        
        public void SetSelected(bool selected)
        {
            _SelectedState.SetState(selected);
        }
    }
}
