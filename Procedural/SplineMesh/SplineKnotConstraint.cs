// Author: František Holubec
// Created: 23.10.2025

#if UNITY_SPLINES
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines;

namespace EDIVE.Procedural.SplineMesh
{
    [ExecuteAlways]
    public class SplineKnotConstraint : MonoBehaviour
    {
        [SerializeField]
        private SplineContainer _SplineContainer;
        
        [DelayedProperty]
        [SerializeField]
        private int _SplineIndex;
        
        [DelayedProperty]
        [SerializeField]
        private int _KnotIndex;
        
        [SerializeField]
        private bool _Continuous;

        private void Update()
        {
            if (_Continuous)
                Apply();
        }

        [Button]
        private void Apply()
        {
            if (_SplineContainer == null || _SplineContainer.Splines.Count == 0)
                return;
            
            var spline = _SplineContainer.Splines[Mathf.Clamp(_SplineIndex, 0, _SplineContainer.Splines.Count)];

            var knotIndex = _KnotIndex.PositiveModulo(spline.Count);
            var containerTransform = _SplineContainer.transform;
            var knot = spline[knotIndex];
            knot.Position = containerTransform.InverseTransformPoint(transform.position);
            knot.Rotation = Quaternion.Inverse(containerTransform.rotation) * transform.rotation;
            
            spline.SetKnot(knotIndex, knot);
        }
    }
}
#endif
