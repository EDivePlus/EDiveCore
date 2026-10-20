// Author: František Holubec
// Created: 22.10.2025

using System;
using UnityEngine;

namespace EDIVE.Procedural.ObjectScaling
{
    [Serializable]
    public abstract class AObjectScalerModule
    {
        public abstract string Label { get; }

        public virtual void Recalculate(ObjectScaler scaler, bool force = false) { }
        public virtual void Postprocess(ObjectScaler scaler) { }
        public virtual void Release(ObjectScaler scaler) { }
        public virtual void SetPreviewOriginal(ObjectScaler scaler, bool preview) { }

        public virtual bool TryCalculateBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            return false;
        }

        // Setup problem shown in the inspector
        public virtual string GetWarning(ObjectScaler scaler) => null;
    }
}
