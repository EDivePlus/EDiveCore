// Author: Michal Petr
// Created: 21.09.2026

using System;
using EDIVE.Rendering.UVDecals;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Avatars.Networking
{
    public class UVDecalDisplay : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _Activation;

        [SerializeField]
        private AToggleState _SelectedState;

        [SerializeField]
        private AToggleState _IsNullState;

        [SerializeField]
        private RawImage _Preview;

        [SerializeField]
        private AspectRatioFitter _PreviewAspectFitter;

        private bool _isSelected;

        public UVDecalPreset Preset { get; private set; }
        public bool IsSelected => _isSelected;

        public event Action<UVDecalDisplay> Selected;

        private void OnEnable()
        {
            _Activation?.RegisterActivationListener(OnActivated);
        }

        private void OnDisable()
        {
            _Activation?.UnregisterActivationListener(OnActivated);
        }

        public void SetPreset(UVDecalPreset preset)
        {
            Preset = preset;
            if (_IsNullState != null)
                _IsNullState.SetState(preset == null);
            RefreshDisplay();
        }

        public void SetSelected(bool selected, bool notify = true)
        {
            _isSelected = selected;
            if (_SelectedState != null)
                _SelectedState.SetState(selected);

            if (selected && notify)
                Selected?.Invoke(this);
        }

        private void OnActivated()
        {
            if (!_isSelected)
                SetSelected(true);
        }

        private void RefreshDisplay()
        {
            if (_Preview == null)
                return;

            var texture = Preset?._Texture;
            _Preview.texture = texture;
            _Preview.enabled = texture != null;

            if (_PreviewAspectFitter != null && texture != null)
                _PreviewAspectFitter.aspectRatio = (float) texture.width / texture.height;
        }
    }
}
