// Author: František Holubec
// Created: 15.10.2021

using EDIVE.DataStructures.VariableFields;
using EDIVE.GeoToolkit.Area;
using EDIVE.GeoToolkit.Coordinates;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.GeoToolkit.Maps
{
    public class MapController : MonoBehaviour
    {
        [Required]
        [EnhancedBoxGroup("Map Source", false)]
        [SerializeReference]
        private IMapSource _Source;

        [EnhancedBoxGroup("Area", false)]
        [SerializeField]
        private VariableField<GeoAreaRect> _Area = new();

        [SerializeField]
        private LayerMask _CastLayerMask;

        [SerializeField]
        private float _CastBias = 0.1f;

        [SerializeField]
        private bool _ShowDebugRays;
        
        [SerializeField]
        private bool _ShowGizmos = true;
        
        public GeoAreaRect GeoArea => _Area.Value;

        [EnhancedFoldoutGroup("Map Transform Data")]
        [InlineIconButton("Refresh", "RecalculateTransformData", GUIAlwaysEnabled = true)]
        [ShowInInspector]
        [HideLabel]
        [InlineProperty]
        public MapTransformData MapTransformData { get; private set; }

        public bool IsValid => _Source?.IsValid ?? false;

        private void Awake()
        {
            RecalculateTransformData();
        }
        
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
            var v = math.dot(originToPoint, MapTransformData.AxisZNormalized) / MapTransformData.Size.z;

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
            
            var startPoint = planePosition + MapTransformData.AxisY + mapNormal * (_CastBias + bias);
            var rayLength = MapTransformData.Size.y + _CastBias + bias * 2;

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

        [OnSceneGUI]
        private void DrawTransformGizmos()
        {
            if (!_ShowGizmos || !IsValid)
                return;

            var data = MapTransformData;
            var origin = (Vector3) data.Origin;
            var axisX = (Vector3) data.AxisX;
            var axisY = (Vector3) data.AxisY;
            var axisZ = (Vector3) data.AxisZ;

            // Eight corners of the (possibly skewed) box spanned by Origin + the three axes.
            var c000 = origin;
            var c100 = origin + axisX;
            var c010 = origin + axisY;
            var c001 = origin + axisZ;
            var c110 = origin + axisX + axisY;
            var c101 = origin + axisX + axisZ;
            var c011 = origin + axisY + axisZ;
            var c111 = origin + axisX + axisY + axisZ;

            Handles.color = new Color(1f, 1f, 1f, 0.5f);
            // Bottom face (AxisY = 0)
            Handles.DrawLine(c000, c100);
            Handles.DrawLine(c100, c101);
            Handles.DrawLine(c101, c001);
            Handles.DrawLine(c001, c000);
            // Top face (AxisY = 1)
            Handles.DrawLine(c010, c110);
            Handles.DrawLine(c110, c111);
            Handles.DrawLine(c111, c011);
            Handles.DrawLine(c011, c010);
            // Vertical edges
            Handles.DrawLine(c000, c010);
            Handles.DrawLine(c100, c110);
            Handles.DrawLine(c001, c011);
            Handles.DrawLine(c101, c111);

            // Origin marker
            var originSize = HandleUtility.GetHandleSize(origin);
            Handles.color = Color.white;
            Handles.SphereHandleCap(0, origin, Quaternion.identity, originSize * 0.08f, EventType.Repaint);
            Handles.Label(origin, $"  Origin\n  {origin:F1}");

            // Axes
            DrawAxis(origin, axisX, Handles.xAxisColor, "X");
            DrawAxis(origin, axisY, Handles.yAxisColor, "Y (Normal)");
            DrawAxis(origin, axisZ, Handles.zAxisColor, "Z");
        }

        private static void DrawAxis(Vector3 origin, Vector3 axis, Color color, string label)
        {
            var length = axis.magnitude;
            if (length < 1e-5f)
                return;

            var direction = axis / length;
            var tip = origin + axis;
            var capSize = HandleUtility.GetHandleSize(tip) * 0.2f;

            Handles.color = color;
            Handles.DrawLine(origin, tip);
            Handles.ArrowHandleCap(0, tip - direction * capSize, Quaternion.LookRotation(direction), capSize, EventType.Repaint);
            Handles.Label(tip, $"  {label}\n  {length:F1}m");
        }
#endif
    }
}
