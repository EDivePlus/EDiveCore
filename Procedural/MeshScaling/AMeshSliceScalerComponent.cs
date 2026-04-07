// Author: František Holubec
// Created: 22.10.2025

using System;
using UnityEngine;

namespace EDIVE.Procedural.MeshScaling
{
    [Serializable]
    public abstract class AMeshSliceScalerComponent
    {
        public abstract string Label { get; }
        
        public virtual void SetPreviewOriginal(bool preview){}
        public virtual bool Recalculate(MeshSliceScaleDetails details, Component container, Transform root, bool force = false) => false;
        public virtual void Postprocess(MeshSliceScaleDetails details, Component container, Transform root){}
        
        public virtual bool TryCalculateBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            return false;
        }

    }
}
