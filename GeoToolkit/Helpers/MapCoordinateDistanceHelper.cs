// Author: František Holubec
// Created: 04.05.2025

using EDIVE.GeoToolkit.Coordinates;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    [ExecuteAlways]
    public class MapCoordinateDistanceHelper : MonoBehaviour
    {
        [SerializeField]
        private MapCoordinatesHelper _First;

        [SerializeField]
        private MapCoordinatesHelper _Second;

        [SerializeField]
        private TMP_Text _Text;

        [SerializeField]
        private float3 _Offset;

        [SerializeField]
        private DistanceMeasureAlgorithm _MeasureAlgorithm = DistanceMeasureAlgorithm.Vincenty;

        [SerializeField]
        private LineRenderer _Line;

        [ShowInInspector]
        public double Distance
        {
            get
            {
                if (_First == null || _Second == null)
                    return 0;

                return _First.GeoCoordinates.DistanceTo(_Second.GeoCoordinates, _MeasureAlgorithm);
            }
        }

        private void LateUpdate()
        {
            if (_Text != null)
            {
                _Text.text = $"{Distance:F2} m";
            }

            if (_Line != null)
            {
                _Line.positionCount = 2;
                _Line.SetPosition(0, _First.transform.TransformPoint(_Offset));
                _Line.SetPosition(1, _Second.transform.TransformPoint(_Offset));
            }
        }
    }
}
