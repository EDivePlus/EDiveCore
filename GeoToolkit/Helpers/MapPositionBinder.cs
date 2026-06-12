// Author: František Holubec
// Created: 03.05.2025

using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    [ExecuteAlways]
    public class MapPositionBinder : MonoBehaviour
    {
        [SerializeField]
        private MapController _SourceMap;

        [SerializeField]
        private MapController _TargetMap;

        [SerializeField]
        private Transform _SourceTransform;

        [SerializeField]
        private bool _SampleHeight;
        
        [ShowIf(nameof(_SampleHeight))]
        [SerializeField]
        private float _HeightSampleBias = 0.01f;

        private void Update()
        {
            if (_SourceMap && _SourceMap.IsValid && _TargetMap && _TargetMap.IsValid && _SourceTransform)
            {
                var sourceCoords = _SourceMap.ConvertToGeoCoordinates(_SourceTransform.position);
                var targetPos = _TargetMap.ConvertToMapCoordinates(sourceCoords);

                if (!_SampleHeight || !_TargetMap.TrySampleHeight(targetPos, out var hitPoint, _HeightSampleBias))
                    hitPoint = transform.position.WithXZ(targetPos.x, targetPos.z);

                transform.position = hitPoint;
            }
        }
    }
}
