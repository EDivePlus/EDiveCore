// Author: František Holubec
// Created: 01.06.2026

using System;
using CoordinateSharp;
using UnityEngine;

namespace EDIVE.GeoToolkit.Coordinates
{
    [Serializable]
    public struct MilitaryGridCoords
    {
        [SerializeField]
        private string _Value;
        public string Value => _Value;
        
        [NonSerialized]
        private MilitaryGridReferenceSystem _cached;
        
        [NonSerialized]
        private bool _parsed;
        
        [NonSerialized]
        private bool _valid;

        public MilitaryGridCoords(string position)
        {
            _Value = position;
            _cached = null;
            _parsed = false;
            _valid = false;
        }

        public MilitaryGridCoords(MilitaryGridReferenceSystem mgrs)
        {
            _Value = mgrs?.ToString();
            _cached = mgrs;
            _parsed = true;
            _valid = mgrs != null;
        }
        
        public bool IsValid => TryGetReferenceSystem(out _);
        
        public bool TryGetReferenceSystem(out MilitaryGridReferenceSystem mgrs)
        {
            if (!_parsed)
            {
                _valid = !string.IsNullOrWhiteSpace(_Value) && MilitaryGridReferenceSystem.TryParse(_Value, out _cached);
                _parsed = true;
            }
            mgrs = _cached;
            return _valid;
        }

        public override string ToString() => _Value;
    }
}
