// Author: František Holubec
// Created: 24.10.2021

using System;
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

namespace EDIVE.GeoToolkit.MapServices
{
    public class WmsMapServiceDefinition : MapServiceDefinition
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

        public override string GenerateURL(string coordinateSystem, string imageFormat, string layerTitle, GeoAreaRect bbox, int2 textureSize)
        {
            if (!TryBeginRequest(layerTitle, out var layer))
                return null;

            if (!CoordinateSystemTypeUtility.TryParse(coordinateSystem, out var requestedSystem))
            {
                Debug.LogError($"[{name}] Cannot generate URL: '{coordinateSystem}' is not a supported coordinate system.", this);
                return null;
            }

            // WMS has no way to reproject, so the area has to already be in the requested CRS.
            if (bbox.CoordinateSystem != requestedSystem)
            {
                Debug.LogError($"[{name}] Cannot generate URL: the area is in {bbox.CoordinateSystem.ToName()} but the request asks for {coordinateSystem}.", this);
                return null;
            }

            var builder = new StringBuilder()
                .Append(RequestLinkWithQuery)
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
        protected override async UniTaskVoid UpdateDataAsync()
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

            var newRequestLink = getMapRequest
                ?.Descendants($"{WMS_NAMESPACE}OnlineResource")
                .FirstOrDefault(e => e.Parent?.Name == $"{WMS_NAMESPACE}Get")
                ?.Attribute("{http://www.w3.org/1999/xlink}href")
                ?.Value;

            if (string.IsNullOrWhiteSpace(newRequestLink))
            {
                SetInvalid("Could not find a GetMap server link in the capabilities document.");
                return;
            }

            // getMapRequest is guaranteed non-null here - newRequestLink was resolved from it above.
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

            var maxWidthElement = service?.Element($"{WMS_NAMESPACE}MaxWidth");
            var maxHeightElement = service?.Element($"{WMS_NAMESPACE}MaxHeight");
            var sizeLimit =
                maxWidthElement != null &&
                maxHeightElement != null &&
                int.TryParse(maxWidthElement.Value, out var maxWidth) &&
                int.TryParse(maxHeightElement.Value, out var maxHeight)
                    ? new int2(maxWidth, maxHeight)
                    : int2.zero;

            SetCapabilities(newRequestLink, imageFormats, coordinateSystems, layers, sizeLimit);
        }
#endif
    }
}
