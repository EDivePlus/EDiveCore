// Author: František Holubec
// Created: 08.11.2021

using EDIVE.GeoToolkit.Coordinates;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    [ExecuteAlways]
    public class MapCoordinatesHelper : MonoBehaviour
    {
        [SerializeField]
        private MapController _Map;

        [SerializeField]
        private bool _ClampPositionToMap;

        [EnableIf(nameof(_ClampPositionToMap))]
        [SerializeField]
        private bool _SampleMapHeight;

        [SerializeField]
        private TMP_Text _Text;

        [SerializeField]
        private CoordinateSystemType _CoordinateSystem = CoordinateSystemType.EPSG_4326;

        public MapController Map
        {
            get => _Map;
            set => _Map = value;
        }

        public CoordinateSystemType CoordinateSystem => _CoordinateSystem;

        [ShowInInspector]
        public GeoCoords GeoCoordinates
        {
            get => _Map
                ? _Map.ConvertToGeoCoordinates(transform.position).ConvertTo(_CoordinateSystem)
                : new GeoCoords(double2.zero, _CoordinateSystem);
            set
            {
                if (_Map == null)
                    return;

                var pos = _Map.ConvertToMapCoordinates(value);
                transform.position = transform.position.WithXZ(pos.x, pos.z);

                if (_ClampPositionToMap)
                    ClampPositionToMap(_SampleMapHeight);
            }
        }

        private void Update()
        {
            if (_Text != null)
            {
                _Text.text = GeoCoordinates.Position.ToString();
            }
        }

        private void LateUpdate()
        {
            if (_ClampPositionToMap)
                ClampPositionToMap(_SampleMapHeight);
        }

        [Button]
        public void ClampPositionToMap(bool sampleHeight = true)
        {
            if (_Map == null)
                return;

            // Clamp the position back inside the map area by round-tripping through clamped geo coordinates.
            var clampedGeo = _Map.ConvertToGeoCoordinates(transform.position, clamp: true);
            var mapPos = _Map.ConvertToMapCoordinates(clampedGeo, sampleHeight);

            // When sampling height, snap fully onto the sampled surface point; otherwise only clamp the XZ plane.
            transform.position = sampleHeight
                ? (Vector3) mapPos
                : transform.position.WithXZ(mapPos.x, mapPos.z);
        }
    }
}
