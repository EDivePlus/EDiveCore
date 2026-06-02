// Author: František Holubec
// Created: 15.10.2021

using EDIVE.DataStructures.VariableFields;
using EDIVE.GeoToolkit.Area;
using EDIVE.GeoToolkit.Coordinates;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    public class MapController : MonoBehaviour
    {
        [Required]
        [SerializeReference]
        [InlineProperty]
        [HideLabel]
        private IMapSource _Source;
        
        [SerializeField]
        private VariableField<GeoAreaRect> _Area = new();
        
        [SerializeField]
        private LayerMask _CastLayerMask;
        
        [SerializeField]
        private float _CastBias = 0.1f;
        
        [SerializeField]
        private bool _ShowDebugRays;

        public GeoAreaRect GeoArea => _Area.Value;
        public MapTransformData MapTransformData { get; private set; }

        public bool IsValid => _Source?.IsValid ?? false;

        private void Awake()
        {
            RecalculateTransformData();
        }

        /// <summary>
        /// Configures the map at runtime with a source and geo area, then refreshes the cached transform data.
        /// </summary>
        public void Initialize(IMapSource source, GeoAreaRect area)
        {
            _Source = source;
            _Area.Value = area;
            RecalculateTransformData();
        }

        [Button]
        public GeoCoords ConvertToGeoCoordinates(float3 worldPosition, bool clamp = false)
        {
            // Project onto map plane
            var mapNormal = MapTransformData.AxisYNormalized;
            var planeProjected = worldPosition - math.dot(worldPosition - MapTransformData.Origin, mapNormal) * mapNormal;

            var originToPoint = planeProjected - MapTransformData.Origin;

            var u = math.dot(originToPoint, MapTransformData.AxisXNormalized) / MapTransformData.Size.x;
            var v = math.dot(originToPoint, MapTransformData.AxisZNormalized) / MapTransformData.Size.y;

            if (clamp)
            {
                u = math.saturate(u);
                v = math.saturate(v);
            }

            return GeoArea.Lerp(new double2(u, v));
        }

        [Button]
        public float3 ConvertToMapCoordinates(GeoCoords coords, bool sampleHeight = false)
        {
            var relativePosition = (float2) GeoArea.InverseLerp(coords);
            var position = MapTransformData.Origin + MapTransformData.AxisX * relativePosition.x + MapTransformData.AxisZ * relativePosition.y;
            if (sampleHeight && TrySampleHeightPlaneProjected(position, out var sampledPosition))
                position = sampledPosition;
            return position;
        }

        public bool TrySampleHeight(float3 worldPosition, out float3 worldPoint, float bias = 0)
        {
            // Project onto a map plane
            var mapNormal = MapTransformData.AxisYNormalized;
            var distanceToPlane = math.dot(worldPosition - MapTransformData.Origin, mapNormal);
            var planeProjected = worldPosition - mapNormal * distanceToPlane;

            return TrySampleHeightPlaneProjected(planeProjected, out worldPoint, bias);
        }

        private bool TrySampleHeightPlaneProjected(float3 planePosition, out float3 worldPoint, float bias = 0)
        {
            worldPoint = planePosition;
            var originToPoint = planePosition - MapTransformData.Origin;

            var u = math.dot(originToPoint, MapTransformData.AxisXNormalized);
            var v = math.dot(originToPoint, MapTransformData.AxisZNormalized);

            if (!u.IsInRange(0, MapTransformData.Size.x , _CastBias) || !v.IsInRange(0, MapTransformData.Size.z, _CastBias))
                return false;

            var mapNormal = MapTransformData.AxisYNormalized;
            var rayDir = -mapNormal;

            var startPoint = planePosition + mapNormal * bias;
            var rayLength = MapTransformData.Size.y + bias * 2;

            if (bias.Approximately(0))
            {
                if (Physics.Raycast(startPoint, rayDir, out var hit, rayLength, _CastLayerMask))
                {
                    if (_ShowDebugRays) Debug.DrawRay(startPoint, rayDir * hit.distance, Color.green, 1);
                    worldPoint = hit.point;
                    return true;
                }
            }
            else
            {
                if (Physics.SphereCast(startPoint, bias, rayDir, out var hit, rayLength, _CastLayerMask))
                {
                    if (_ShowDebugRays) Debug.DrawRay(startPoint, rayDir * rayLength, Color.yellow, 1);
                    worldPoint = hit.point;
                    return true;
                }
            }

            if (_ShowDebugRays) Debug.DrawRay(startPoint, rayDir * rayLength, Color.red, 1);
            return false;
        }
        
        [Button]
        public void RecalculateTransformData()
        {
            if (!IsValid)
            {
                Debug.LogWarning($"[{nameof(MapController)}] No valid {nameof(IMapSource)} assigned.", this);
                return;
            }

            MapTransformData = _Source.CalculateTransformData(transform);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (IsValid)
                RecalculateTransformData();
        }
#endif
    }
}
