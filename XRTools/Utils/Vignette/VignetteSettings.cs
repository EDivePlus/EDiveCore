// Author: František Holubec
// Created: 08.07.2026

using System;
using UnityEngine;

namespace EDIVE.XRTools.Utils.Vignette
{
    [Serializable]
    public class VignetteSettings
    {
        [SerializeField, Range(0f, 1f)]
        private float _ApertureSize = 1f;

        [SerializeField, Range(0f, 1f)]
        private float _Feathering;

        [SerializeField]
        private Color _Color = Color.black;

        [SerializeField]
        private Color _ColorBlend = Color.black;

        [SerializeField]
        private float _VerticalPosition;

        public float ApertureSize { get => _ApertureSize; set => _ApertureSize = value; }
        public float Feathering { get => _Feathering; set => _Feathering = value; }
        public Color Color { get => _Color; set => _Color = value; }
        public Color ColorBlend { get => _ColorBlend; set => _ColorBlend = value; }
        public float VerticalPosition { get => _VerticalPosition; set => _VerticalPosition = value; }
        
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
            _Color = other._Color;
            _ColorBlend = other._ColorBlend;
            _VerticalPosition = other._VerticalPosition;
        }
        
        public static void Lerp(VignetteSettings a, VignetteSettings b, float t, VignetteSettings result)
        {
            result._ApertureSize = Mathf.Lerp(a._ApertureSize, b._ApertureSize, t);
            result._Feathering = Mathf.Lerp(a._Feathering, b._Feathering, t);
            result._Color = Color.Lerp(a._Color, b._Color, t);
            result._ColorBlend = Color.Lerp(a._ColorBlend, b._ColorBlend, t);
            result._VerticalPosition = Mathf.Lerp(a._VerticalPosition, b._VerticalPosition, t);
        }
    }
}
