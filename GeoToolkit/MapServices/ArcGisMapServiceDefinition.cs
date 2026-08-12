using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using EDIVE.GeoToolkit.Area;
using EDIVE.GeoToolkit.Coordinates;
using EDIVE.Utils.SerializableDictionary;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.GeoToolkit.MapServices
{
    public class ArcGisMapServiceDefinition : MapServiceDefinition
    {
        private const int DESCRIPTOR_TIMEOUT_SECONDS = 30;
        private const string IMAGE_SERVER_SUFFIX = "/ImageServer";
        private const string MAP_SERVER_SUFFIX = "/MapServer";

        // exportImage accepts these regardless of the descriptor, which does not advertise a format list.
        private static readonly List<string> IMAGE_SERVER_FORMATS = new()
        {
            "tiff", "lerc", "png", "png8", "png24", "png32", "jpg", "jpgpng", "bmp", "gif", "bsq"
        };

        [InfoBox("$_StatusMessage", VisibleIf = nameof(_IsValid))]
        [InfoBox("$_StatusMessage", InfoMessageType.Error, VisibleIf = "@!_IsValid && !string.IsNullOrEmpty(_StatusMessage)")]
        [SerializeField]
        [TextArea(1, 5)]
        [PropertyOrder(-1)]
        [Tooltip("Service root, ending in /ImageServer or /MapServer.")]
        private string _ServiceLink;

        [SerializeField]
        [HideInInspector]
        private bool _IsImageServer;

        public override string GenerateURL(string coordinateSystem, string imageFormat, string layerTitle, GeoAreaRect bbox, int2 textureSize)
        {
            if (!TryBeginRequest(layerTitle, out var layer))
                return null;

            var imageSR = CoordinateSystemTypeUtility.Parse(coordinateSystem).ToEpsgCode();
            if (imageSR == 0)
            {
                Debug.LogError($"[{name}] Cannot generate URL: '{coordinateSystem}' is not a supported coordinate system.", this);
                return null;
            }

            // Unlike WMS, ArcGIS reprojects server side, so the area only has to be in a system we can name.
            var bboxSR = bbox.CoordinateSystem.ToEpsgCode();
            if (bboxSR == 0)
            {
                Debug.LogError($"[{name}] Cannot generate URL: the area is in an unsupported coordinate system.", this);
                return null;
            }

            var builder = new StringBuilder()
                .Append(RequestLinkWithQuery)
                .Append($"bbox={bbox.ToCommaSeparatedString()}")
                .Append($"&bboxSR={bboxSR}")
                .Append($"&imageSR={imageSR}")
                .Append($"&size={textureSize.x},{textureSize.y}")
                .Append($"&format={Uri.EscapeDataString(imageFormat)}")
                .Append("&f=image");

            if (_IsImageServer)
                builder.Append($"&renderingRule={Uri.EscapeDataString($"{{\"rasterFunction\":\"{layer}\"}}")}");
            else
                builder.Append($"&layers={Uri.EscapeDataString($"show:{layer}")}").Append("&transparent=false");

            return builder.ToString();
        }

#if UNITY_EDITOR
        protected override async UniTaskVoid UpdateDataAsync()
        {
            if (string.IsNullOrWhiteSpace(_ServiceLink))
            {
                SetInvalid("No service link provided.");
                return;
            }

            var serviceLink = _ServiceLink.TrimEnd('/', '?');
            var isImageServer = serviceLink.EndsWith(IMAGE_SERVER_SUFFIX, StringComparison.OrdinalIgnoreCase);
            if (!isImageServer && !serviceLink.EndsWith(MAP_SERVER_SUFFIX, StringComparison.OrdinalIgnoreCase))
            {
                SetInvalid($"'{serviceLink}' does not end in {IMAGE_SERVER_SUFFIX} or {MAP_SERVER_SUFFIX}.");
                return;
            }

            JObject descriptor;
            try
            {
                using var webRequest = UnityWebRequest.Get($"{serviceLink}?f=json");
                webRequest.timeout = DESCRIPTOR_TIMEOUT_SECONDS;
                await webRequest.SendWebRequest();
                descriptor = JObject.Parse(webRequest.downloadHandler.text);
            }
            catch (Exception e)
            {
                SetInvalid($"Failed to load service descriptor from '{serviceLink}': {e.Message}");
                return;
            }

            // ArcGIS reports failures as a JSON body with HTTP 200.
            var error = descriptor["error"];
            if (error != null)
            {
                SetInvalid($"The service returned an error: {error.Value<string>("message")}");
                return;
            }

            var layers = new SerializableDictionary<string, string>();
            List<string> imageFormats;

            if (isImageServer)
            {
                imageFormats = IMAGE_SERVER_FORMATS;
                foreach (var rasterFunction in descriptor["rasterFunctionInfos"] ?? Enumerable.Empty<JToken>())
                {
                    var functionName = rasterFunction.Value<string>("name");
                    if (string.IsNullOrWhiteSpace(functionName) || layers.ContainsKey(functionName))
                        continue;
                    layers.Add(functionName, functionName);
                }

                // Every ImageServer can serve raw values even when it advertises no raster functions.
                if (layers.Count == 0)
                    layers.Add("None", "None");
            }
            else
            {
                imageFormats = (descriptor.Value<string>("supportedImageFormatTypes") ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                foreach (var layer in descriptor["layers"] ?? Enumerable.Empty<JToken>())
                {
                    var layerName = layer.Value<string>("name");
                    if (string.IsNullOrWhiteSpace(layerName) || layers.ContainsKey(layerName))
                        continue;
                    layers.Add(layerName, layer.Value<int>("id").ToString());
                }
            }

            if (imageFormats.Count == 0)
            {
                SetInvalid("The service advertises no image formats.");
                return;
            }

            if (layers.Count == 0)
            {
                SetInvalid("The service advertises no layers.");
                return;
            }

            // ArcGIS transforms to whatever is asked for, so offer everything the toolkit can name.
            var coordinateSystems = Enum.GetValues(typeof(CoordinateSystemType))
                .Cast<CoordinateSystemType>()
                .Where(type => type != CoordinateSystemType.Unknown)
                .Select(type => type.ToName())
                .ToList();

            var sizeLimit = new int2(descriptor.Value<int?>("maxImageWidth") ?? 0, descriptor.Value<int?>("maxImageHeight") ?? 0);

            _IsImageServer = isImageServer;
            SetCapabilities(serviceLink + (isImageServer ? "/exportImage" : "/export"), imageFormats, coordinateSystems, layers, sizeLimit);
        }
#endif
    }
}
