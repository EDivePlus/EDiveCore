// Author: František Holubec
// Created: 08.07.2026

using System;
using UnityEngine;

namespace EDIVE.XRTools.Utils.Vignette
{
    [Serializable]
    public class VignetteSettings
    {
        [Range(-1f, 1f)]
        [SerializeField] 
        private float _ApertureSize = 1f;

        [Range(0f, 1f)]
        [SerializeField] 
        private float _Feathering;

        [Range(0f, 1f)]
        [SerializeField] 
        private float _Alpha = 1f;

        [SerializeField]
        private Color _Color = Color.white;

        [SerializeField]
        private Gradient _Gradient = CreateDefaultGradient();

        [SerializeField]
        private float _VerticalPosition;

        public float ApertureSize
        {
            get => _ApertureSize;
            set => _ApertureSize = value;
        }
        public float Feathering
        {
            get => _Feathering;
            set => _Feathering = value;
        }
        public float Alpha
        {
            get => _Alpha;
            set => _Alpha = value;
        }
        public Color Color
        {
            get => _Color;
            set => _Color = value;
        }
        public Gradient Gradient
        {
            get => _Gradient;
            set => _Gradient = value;
        }
        public float VerticalPosition
        {
            get => _VerticalPosition;
            set => _VerticalPosition = value;
        }

        private static Gradient CreateDefaultGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] {new GradientColorKey(Color.black, 0f)}, new[] {new GradientAlphaKey(1f, 0f)});
            return gradient;
        }

        public static VignetteSettings None => new();

        public static VignetteSettings Default => new()
        {
            ApertureSize = 0.7f,
            Feathering = 0.2f,
        };

        public void CopyFrom(VignetteSettings other)
        {
            _ApertureSize = other._ApertureSize;
            _Feathering = other._Feathering;
            _Alpha = other._Alpha;
            _Color = other._Color;
            _Gradient = other._Gradient;
            _VerticalPosition = other._VerticalPosition;
        }

        public static void Lerp(VignetteSettings a, VignetteSettings b, float t, VignetteSettings result)
        {
            result._ApertureSize = Mathf.Lerp(a._ApertureSize, b._ApertureSize, t);
            result._Feathering = Mathf.Lerp(a._Feathering, b._Feathering, t);
            result._Alpha = Mathf.Lerp(a._Alpha, b._Alpha, t);
            result._Color = Color.Lerp(a._Color, b._Color, t);
            result._Gradient = t < 0.5f ? a._Gradient : b._Gradient;
            result._VerticalPosition = Mathf.Lerp(a._VerticalPosition, b._VerticalPosition, t);
        }
    }
}
