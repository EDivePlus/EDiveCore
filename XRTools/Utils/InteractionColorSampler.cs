// Author: František Holubec

using System;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.XRTools.Utils
{
    public class InteractionColorSampler : MonoBehaviour
    {
        [Required]
        [SerializeField]
        protected XRBaseInteractable _Interactable;
        
        [EnhancedValidate("ValidateTexture")]
        [SerializeField]
        private Texture2D _Texture;

        [Required]
        [SerializeField]
        private MeshCollider _MeshCollider;

        [ShowInInspector]
        public Color CurrentSampleColor
        {
            get => _currentSampleColor;
            set
            {
                if (_currentSampleColor == value) return;
                _currentSampleColor = value;
                ColorSampleChanged?.Invoke(_currentSampleColor);
            }
        }

        private IXRHoverInteractor _currentInteractor;
        private Color _currentSampleColor;

        public event Action<Color> ColorSampleChanged;
        public event Action<Color> ColorSelected;

        private void OnEnable()
        {
            if (_Interactable == null)
                return;

            _Interactable.hoverEntered.AddListener(OnHoverEntered);
            _Interactable.hoverExited.AddListener(OnHoverExited);
            _Interactable.selectExited.AddListener(OnSelected);
        }

        private void OnDisable()
        {
            if (_Interactable == null)
                return;

            _Interactable.hoverEntered.RemoveListener(OnHoverEntered);
            _Interactable.hoverExited.RemoveListener(OnHoverExited);
            _Interactable.selectExited.RemoveListener(OnSelected);
        }
        
        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            _currentInteractor = args.interactorObject;
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            _currentInteractor = null;
        }

        private void OnSelected(SelectExitEventArgs args)
        {
            ColorSelected?.Invoke(CurrentSampleColor);
        }

        private void Update()
        {
            if (_currentInteractor != null && _currentInteractor.TryGetCurrentRaycastTarget(_MeshCollider, out var raycastHit))
            {
                var textureCoord = raycastHit.textureCoord;
                var newX = Mathf.RoundToInt(textureCoord.x * _Texture.width);
                var newY = Mathf.RoundToInt(textureCoord.y * _Texture.height);
                CurrentSampleColor = _Texture.GetPixel(newX, newY);
            }
            else
            {
                CurrentSampleColor = Color.clear;
            }
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateTexture(Texture2D value, SelfValidationResult result)
        {
            if (value != null && !value.IsTextureReadable())
            {
                result.AddError($"Texture '{value.name}' is not readable!")
                    .WithFix(() => value.SetTextureReadable(true));
            }
        }
#endif
    }
}
