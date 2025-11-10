// Author: František Holubec
// Created: 10.11.2025

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils
{
    public class GradientMaterialAssigner : MonoBehaviour
    {
        [SerializeField]
        [OnValueChanged(nameof(UpdateGradientTexture), true)]
        private Gradient _Gradient;
        
        [SerializeField]
        private Renderer _Renderer;
        
        [SerializeField]
        private string _TexturePropertyName = "_Gradient";
        
        [SerializeField]
        [OnValueChanged(nameof(UpdateGradientTexture), true)]
        private int _Size = 256;
        
        [ReadOnly]
        [EnableGUI]
        [EnhancedPreviewField]
        [SerializeField]
        private Texture2D _GradientTexture;
        
        private void Awake()
        {
            UpdateGradientTexture();
        }

        [Button]
        public void UpdateGradientTexture()
        {
            if (_Renderer == null)
                return;
            if (_GradientTexture != null)

                DestroyImmediate(_GradientTexture);
            _GradientTexture = GenerateGradientTexture(_Gradient);
            
            var material = _Renderer.material;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                material = _Renderer.sharedMaterial;
#endif
            material.SetTexture(_TexturePropertyName, _GradientTexture);
        }

        private Texture2D GenerateGradientTexture(Gradient grad)
        {
            var gradientTexture = new Texture2D(_Size, 1);
            for (var x = 0; x < _Size; x++)
            {
                var color = grad.Evaluate( x / (float) _Size);
                gradientTexture.SetPixel(x, 0, color);
            }
            gradientTexture.wrapMode = TextureWrapMode.Clamp;
            gradientTexture.Apply();
            return gradientTexture;
        }
    }
}
