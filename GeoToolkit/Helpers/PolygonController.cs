// Author: František Holubec
// Created: 10.11.2021

using System.Collections.Generic;
using EDIVE.Utils.SerializableDictionary;
using UnityEngine;

namespace EDIVE.GeoToolkit.Maps
{
    public class PolygonController : MonoBehaviour
    {
        [SerializeField]
        private SerializableDictionary<string, string> properties = new SerializableDictionary<string, string>();

        public IDictionary<string, string> Properties
        {
            get => properties;
            set => properties = new SerializableDictionary<string, string>(value);
        }
    }
}
