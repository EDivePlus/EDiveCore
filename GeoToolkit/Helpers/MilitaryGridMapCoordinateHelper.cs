// Author: František Holubec
// Created: 03.05.2025

using EDIVE.GeoToolkit.Coordinates;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.GeoToolkit.Maps
{
    [ExecuteAlways]
    public class MilitaryGridMapCoordinateHelper : MonoBehaviour
    {
        [SerializeField]
        private MapController _Map;

        [SerializeField]
        private TMP_Text _Text;

        [SerializeField]
        private TMP_InputField _Input;

        [SerializeField]
        private Button _ResetButton;

        [SerializeField]
        private Button _ApplyButton;

        public MapController Map
        {
            get => _Map;
            set => _Map = value;
        }

        [ShowInInspector]
        [DelayedProperty]
        public string Coordinates
        {
            get => _Map
                ? _Map.ConvertToGeoCoordinates(transform.position).ConvertToMilitaryGrid().Value
                : string.Empty;
            set
            {
                if (_Map == null)
                    return;

                if (new MilitaryGridCoords(value).TryConvertToGeoCoords(CoordinateSystemType.EPSG_4326, out var coords))
                {
                    var pos = _Map.ConvertToMapCoordinates(coords);
                    transform.position = transform.position.WithXZ(pos.x, pos.z);
                    _Text.text = value;
                }
            }
        }

        private void Awake()
        {
            if (_Input)
            {
                transform.AddChangeListener(OnTransformChanged);
                if (_ResetButton) _ResetButton.onClick.AddListener(OnResetButtonClicked);
                if (_ApplyButton) _ApplyButton.onClick.AddListener(OnApplyButtonClicked);
            }
        }

        private void Update()
        {
            if (_Text)
            {
                _Text.text = Coordinates;
            }
        }

        private void OnApplyButtonClicked()
        {
            Coordinates = _Input.text;
        }

        private void OnResetButtonClicked()
        {
            _Input.text = Coordinates;
        }

        private void OnTransformChanged(Transform obj)
        {
            _Input.text = Coordinates;
        }
    }
}
