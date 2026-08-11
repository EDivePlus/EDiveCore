// Author: František Holubec
// Created: 24.10.2021

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.GeoToolkit.Area;
using EDIVE.GeoToolkit.Coordinates;
using EDIVE.Utils.SerializableDictionary;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace EDIVE.GeoToolkit.WebMapServices
{
    [CreateAssetMenu(fileName = "NewWMSServerCapabilities", menuName = "WMS/WMS Server Capabilities")]
    public class WebMapServiceServerDefinition : ScriptableObject
    {
        private const string WMS_NAMESPACE = "{http://www.opengis.net/wms}";
        private const int CAPABILITIES_TIMEOUT_SECONDS = 30;
        
        [InfoBox("$_StatusMessage", VisibleIf = nameof(_IsValid))]
        [InfoBox("$_StatusMessage", InfoMessageType.Error, VisibleIf = "@!_IsValid && !string.IsNullOrEmpty(_StatusMessage)")]
        [FormerlySerializedAs("capabilitiesXMLLink")]
        [SerializeField]
        [TextArea(1, 5)]
        [PropertyOrder(-1)]
        private string _CapabilitiesXMLLink;
        
        [FormerlySerializedAs("serverLink")]
        [PropertySpace]
        [SerializeField] 
        [TextArea(1, 5)]
        private string _ServerLink;
        
        [FormerlySerializedAs("imageFormats")]
        [PropertySpace]
        [SerializeField]
        private List<string> _ImageFormats;
        
        [FormerlySerializedAs("coordinateSystems")]
        [SerializeField]
        private List<string> _CoordinateSystems;
        
        [FormerlySerializedAs("layers")]
        [SerializeField]
        private SerializableDictionary<string, string> _Layers;
        
        [FormerlySerializedAs("sizeLimit")]
        [SerializeField]
        private int2 _SizeLimit;

        [SerializeField]
        [HideInInspector]
        private bool _IsValid;

        [SerializeField]
        [HideInInspector]
        private string _StatusMessage;

        public string ServerLink => _ServerLink;
        public IEnumerable<string> ImageFormats => _ImageFormats;
        public IEnumerable<string> CoordinateSystems => _CoordinateSystems;
        public IEnumerable<string> Layers => _Layers != null ? _Layers.Keys : new List<string>();
        public int2 SizeLimit => _SizeLimit;

        /// <summary>
        /// Whether the last <see cref="UpdateData"/> call produced a complete, usable definition.
        /// </summary>
        public bool IsValid => _IsValid;
        
        public string GenerateURL(string coordinateSystem, string imageFormat, string layerTitle, GeoAreaRect bbox, int2 textureSize)
        {
            if (!_IsValid || string.IsNullOrEmpty(_ServerLink))
            {
                Debug.LogError($"[{name}] Cannot generate URL: the server definition is not valid. Run 'Update Data' first.", this);
                return null;
            }

            if (_Layers == null || !_Layers.TryGetValue(layerTitle, out var layer))
            {
                Debug.LogError($"[{name}] Cannot generate URL: unknown layer '{layerTitle}'.", this);
                return null;
            }

            if (!CoordinateSystemTypeUtility.TryParse(coordinateSystem, out var requestedSystem))
            {
                Debug.LogError($"[{name}] Cannot generate URL: '{coordinateSystem}' is not a supported coordinate system.", this);
                return null;
            }

            if (bbox.CoordinateSystem != requestedSystem)
            {
                Debug.LogError($"[{name}] Cannot generate URL: the area is in {bbox.CoordinateSystem.ToName()} but the request asks for {coordinateSystem}.", this);
                return null;
            }

            var builder = new StringBuilder()
                .Append($"{ServerLink}")
                .Append(_ServerLink[^1] == '?' ? "" : "?")
                .Append("request=GetMap")
                .Append("&service=WMS")
                .Append("&version=1.3.0")
                .Append($"&CRS={Uri.EscapeDataString(coordinateSystem)}")
                .Append($"&format={Uri.EscapeDataString(imageFormat)}")
                .Append("&styles=")
                .Append($"&Layers={Uri.EscapeDataString(layer)}")
                .Append($"&BBOX={bbox.ToCommaSeparatedString(requestedSystem.IsNorthFirstAxisOrder())}")
                .Append($"&WIDTH={textureSize.x}&HEIGHT={textureSize.y}");
            return builder.ToString();
        }

#if UNITY_EDITOR
        [PropertyOrder(-1)]
        [Button]
        private void UpdateData()
        {
            UniTask.Void(UpdateDataAsync);
        }

        private async UniTaskVoid UpdateDataAsync()
        {
            if (string.IsNullOrWhiteSpace(_CapabilitiesXMLLink))
            {
                SetInvalid("No capabilities XML link provided.");
                return;
            }

            // Unreachable hosts, HTTP errors and malformed XML all surface here - never let them bubble up.
            XDocument document;
            try
            {
                using var webRequest = UnityWebRequest.Get(_CapabilitiesXMLLink);
                webRequest.timeout = CAPABILITIES_TIMEOUT_SECONDS;
                await webRequest.SendWebRequest();
                document = XDocument.Parse(webRequest.downloadHandler.text);
            }
            catch (Exception e)
            {
                SetInvalid($"Failed to load capabilities XML from '{_CapabilitiesXMLLink}': {e.Message}");
                return;
            }

            var capabilities = document.Element($"{WMS_NAMESPACE}WMS_Capabilities");
            if (capabilities == null)
            {
                SetInvalid("The loaded document is not a valid WMS Capabilities document (missing WMS_Capabilities root element).");
                return;
            }

            var service = capabilities.Element($"{WMS_NAMESPACE}Service");
            var capability = capabilities.Element($"{WMS_NAMESPACE}Capability");

            var getMapRequest = capability
                ?.Element($"{WMS_NAMESPACE}Request")
                ?.Element($"{WMS_NAMESPACE}GetMap");

            var newServerLink = getMapRequest
                ?.Descendants($"{WMS_NAMESPACE}OnlineResource")
                .FirstOrDefault(e => e.Parent?.Name == $"{WMS_NAMESPACE}Get")
                ?.Attribute("{http://www.w3.org/1999/xlink}href")
                ?.Value;

            if (string.IsNullOrWhiteSpace(newServerLink))
            {
                SetInvalid("Could not find a GetMap server link in the capabilities document.");
                return;
            }

            // getMapRequest is guaranteed non-null here - newServerLink was resolved from it above.
            var imageFormats = getMapRequest
                .Elements($"{WMS_NAMESPACE}Format")
                .Select(e => e.Value)
                .Distinct()
                .ToList();

            if (imageFormats.Count == 0)
            {
                SetInvalid("No image formats found for the GetMap request.");
                return;
            }

            var coordinateSystems = capability
                .Element($"{WMS_NAMESPACE}Layer")
                ?.Descendants($"{WMS_NAMESPACE}CRS")
                .Select(e => e.Value)
                .Distinct()
                .ToList();

            if (coordinateSystems == null || coordinateSystems.Count == 0)
            {
                SetInvalid("No coordinate systems (CRS) found in the capabilities document.");
                return;
            }

            // A layer is requestable when it has a Name - 'queryable' only advertises GetFeatureInfo support.
            var layers = new SerializableDictionary<string, string>();
            foreach (var layerElement in capability.Descendants($"{WMS_NAMESPACE}Layer"))
            {
                var layerName = layerElement.Element($"{WMS_NAMESPACE}Name")?.Value;
                if (string.IsNullOrWhiteSpace(layerName))
                    continue;

                var title = layerElement.Element($"{WMS_NAMESPACE}Title")?.Value;
                var key = string.IsNullOrWhiteSpace(title) ? layerName : title;
                if (layers.ContainsKey(key))
                    continue;
                layers.Add(key, layerName);
            }

            if (layers.Count == 0)
            {
                SetInvalid("No named layers found in the capabilities document.");
                return;
            }

            // Everything parsed successfully - commit the new values.
            if (_ServerLink != newServerLink)
                ClearData();

            _ServerLink = newServerLink;
            _ImageFormats = imageFormats;
            _CoordinateSystems = coordinateSystems;
            _Layers = layers;

            var maxWidthElement = service?.Element($"{WMS_NAMESPACE}MaxWidth");
            var maxHeightElement = service?.Element($"{WMS_NAMESPACE}MaxHeight");
            _SizeLimit =
                maxWidthElement != null &&
                maxHeightElement != null &&
                int.TryParse(maxWidthElement.Value, out var maxWidth) &&
                int.TryParse(maxHeightElement.Value, out var maxHeight)
                    ? new int2(maxWidth, maxHeight)
                    : int2.zero;

            _IsValid = true;
            _StatusMessage = $"Valid: {layers.Count} layer(s), {imageFormats.Count} image format(s), {coordinateSystems.Count} coordinate system(s).";
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void SetInvalid(string message)
        {
            Debug.LogError($"[{name}] WMS update failed - {message}", this);
            ClearData();
            _IsValid = false;
            _StatusMessage = message;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void ClearData()
        {
            _ServerLink = null;
            _ImageFormats?.Clear();
            _CoordinateSystems?.Clear();
            _Layers?.Clear();
            _SizeLimit = int2.zero;
        }
#endif
    }
}
