// Author: František Holubec
// Created: 22.10.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Procedural.MeshScaling
{
    [Serializable]
    public class MeshScalerDetails
    {
        [SerializeField]
        private Vector3 _TargetSize = Vector3.one;

        [SerializeField]
        private Vector3 _SliceStart = Vector3.zero;

        [SerializeField]
        private Vector3 _SliceEnd = Vector3.zero;

        [Header("Bounds")]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private Bounds _Bounds;

        public ref Vector3 TargetSize => ref _TargetSize;
        public ref Vector3 SliceStart => ref _SliceStart;
        public ref Vector3 SliceEnd => ref _SliceEnd;
        public ref Bounds Bounds => ref _Bounds;
    }
}
