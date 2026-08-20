using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.GeoToolkit.Area;
using EDIVE.GeoToolkit.Utils;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using Progress = Cysharp.Threading.Tasks.Progress;

namespace EDIVE.GeoToolkit.MapServices
{
    [Serializable]
    public class MapServiceRequest
    {
        [FormerlySerializedAs("serverDefinition")]
        [FormerlySerializedAs("_ServerDefinition")]
        [SerializeField]
        [ShowCreateNew]
        [EnhancedInlineEditor]
        private MapServiceDefinition _ServiceDefinition;

        [FormerlySerializedAs("imageFormat")]
        [PropertySpace]
        [SerializeField]
        [ValidateInput("IsImageFormatValid", "The server does not offer this image format.")]
        [ValueDropdown(nameof(AvailableImageFormats), FlattenTreeView = true)]
        private string _ImageFormat;

        [FormerlySerializedAs("coordinateSystem")]
        [SerializeField]
        [ValidateInput("IsCoordinateSystemValid", "The server does not offer this coordinate system.")]
        [ValueDropdown(nameof(AvailableCoordinateSystems), FlattenTreeView = true)]
        private string _CoordinateSystem;

        [FormerlySerializedAs("layer")]
        [SerializeField]
        [ValidateInput("IsLayerValid", "The server does not offer this layer.")]
        [ValueDropdown(nameof(AvailableLayers), FlattenTreeView = true)]
        private string _Layer;

        private IEnumerable<string> AvailableImageFormats => _ServiceDefinition ? _ServiceDefinition.ImageFormats : new List<string>();
        private IEnumerable<string> AvailableCoordinateSystems => _ServiceDefinition ? _ServiceDefinition.CoordinateSystems : new List<string>();
        private IEnumerable<string> AvailableLayers => _ServiceDefinition ? _ServiceDefinition.Layers : new List<string>();

        public bool IsSizeLimited => SizeLimit.x != 0 && SizeLimit.y != 0;
        public int2 SizeLimit => _ServiceDefinition ? _ServiceDefinition.SizeLimit : int2.zero;

        public async UniTask<MapServiceTextureResult> DownloadAsync(GeoAreaRect geoArea, int2 size, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (IsSizeLimited && (size.x > SizeLimit.x || size.y > SizeLimit.y))
                return await DownloadPartWiseAsync(geoArea, size, progress, cancellationToken);

            return await DownloadAsOneAsync(geoArea, size, progress, cancellationToken);
        }

        private string GenerateURL(GeoAreaRect geoArea, int2 textureSize)
        {
            return _ServiceDefinition.GenerateURL(_CoordinateSystem, _ImageFormat, _Layer, geoArea, textureSize);
        }

        private async UniTask<MapServiceTextureResult> DownloadPartWiseAsync(GeoAreaRect geoArea, int2 size, IProgress<float> progress, CancellationToken cancellationToken)
        {
            var subAreas = geoArea.Split(size, SizeLimit);
            var partGrid = MapServiceTextureResult.GetPartGrid(size, SizeLimit);
            var partGridDimensions = new int2(partGrid.GetLength(0), partGrid.GetLength(1));
            var partsCount = partGridDimensions.x * partGridDimensions.y;

            var parts = new MapServiceTextureResult.Part[partGridDimensions.x, partGridDimensions.y];

            for (var x = 0; x < partGridDimensions.x; x++)
            {
                for (var y = 0; y < partGridDimensions.y; y++)
                {
                    var dimensions = partGrid[x, y];
                    var partIndex = x * partGridDimensions.y + y;
                    var url = GenerateURL(subAreas[x, y], dimensions);

                    var partProgress = progress == null
                        ? null
                        : Progress.Create<float>(p => progress.Report((partIndex + p) / partsCount));

                    var data = await DownloadAsync(url, partProgress, cancellationToken);
                    parts[x, y] = new MapServiceTextureResult.Part(data, dimensions);
                }
            }

            progress?.Report(1f);
            return new MapServiceTextureResult(parts, size, SizeLimit);
        }

        private async UniTask<MapServiceTextureResult> DownloadAsOneAsync(GeoAreaRect geoArea, int2 size, IProgress<float> progress, CancellationToken cancellationToken)
        {
            var url = GenerateURL(geoArea, size);
            var data = await DownloadAsync(url, progress, cancellationToken);

            var parts = new MapServiceTextureResult.Part[1, 1];
            parts[0, 0] = new MapServiceTextureResult.Part(data, size);
            return new MapServiceTextureResult(parts, size, SizeLimit);
        }

        private static async UniTask<byte[]> DownloadAsync(string url, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("No URL provided.", nameof(url));

            using var webRequest = UnityWebRequest.Get(url);
            try
            {
                await webRequest.SendWebRequest().ToUniTask(progress, cancellationToken: cancellationToken);
            }
            catch (UnityWebRequestException e)
            {
                throw new MapServiceException(url, webRequest.responseCode, DescribeFailure(webRequest.downloadHandler?.text), e);
            }

            // Both WMS and ArcGIS report failures in the body with HTTP 200, and ArcGIS still labels them image/tiff,
            // so trust the magic bytes rather than the header.
            var data = webRequest.downloadHandler.data;
            var imageFormat = data.GetImageFormat();
            if (imageFormat is null or "xml")
                throw new MapServiceException(url, webRequest.responseCode, DescribeFailure(webRequest.downloadHandler.text));

            return data;
        }

        private static string DescribeFailure(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            // WMS answers with a ServiceExceptionReport, ArcGIS with JSON or an HTML error page.
            var serviceException = Regex.Match(body, "<ServiceException[^>]*>(.*?)</ServiceException>", RegexOptions.Singleline);
            if (serviceException.Success)
                return Tidy(serviceException.Groups[1].Value);

            var jsonMessage = Regex.Match(body, "\"message\"\\s*:\\s*\"(.*?)\"");
            if (jsonMessage.Success)
                return Tidy(jsonMessage.Groups[1].Value);

            var text = Tidy(Regex.Replace(body, "<[^>]+>", " "));
            return text.Length > 300 ? text[..300] + "..." : text;
        }

        private static string Tidy(string value) => Regex.Replace(value, @"\s+", " ").Trim();


#if UNITY_EDITOR
        [FormerlySerializedAs("_TestAreaRect")]
        [FormerlySerializedAs("_TestAreaBoundingBox")]
        [FormerlySerializedAs("testAreaBBox")]
        [SerializeField]
        [PropertyOrder(10)]
        [EnhancedFoldoutGroup("Test", "@Color.yellow")]
        [LabelText("Area Bounding Box")]
        private GeoAreaRect _TestGeoAreaRect;

        [FormerlySerializedAs("testTextureSize")]
        [PropertySpace(4)]
        [SerializeField]
        [PropertyOrder(10)]
        [EnhancedFoldoutGroup("Test")]
        [LabelText("Texture Size")]
        private int2 _TestTextureSize;

        [PropertySpace(4)]
        [PropertyOrder(11)]
        [Button("Generate URL", ButtonStyle.FoldoutButton, Expanded = true)]
        [EnhancedFoldoutGroup("Test")]
        private string GenerateURLTest()
        {
            return GenerateURL(_TestGeoAreaRect, _TestTextureSize);
        }

        [PropertySpace]
        [PropertyOrder(11)]
        [Button("Download")]
        [EnhancedFoldoutGroup("Test")]
        private void DownloadData()
        {
            UniTask.Void(async () =>
            {
                using var cancellationSource = new CancellationTokenSource();
                var progress = Progress.Create<float>(p =>
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Downloading", "Downloading Raster", p))
                        cancellationSource.Cancel();
                });

                try
                {
                    var data = await DownloadAsync(GenerateURL(_TestGeoAreaRect, _TestTextureSize), progress, cancellationSource.Token);
                    EditorUtility.ClearProgressBar();
                    TrySaveToFile(data);
                }
                catch (OperationCanceledException)
                {
                    EditorUtility.ClearProgressBar();
                }
            });
        }

        private static void TrySaveToFile(byte[] data)
        {
            if (data == null)
                return;

            var extension = data.GetImageFormat();
            var filePath = EditorUtility.SaveFilePanel("Save file", "", $"result{(extension == null ? "" : ".")}{extension}", extension);
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            File.WriteAllBytes(filePath, data);
        }

        [UsedImplicitly]
        private bool IsLayerValid => AvailableLayers.Contains(_Layer);

        [UsedImplicitly]
        private bool IsCoordinateSystemValid => AvailableCoordinateSystems.Contains(_CoordinateSystem);

        [UsedImplicitly]
        private bool IsImageFormatValid => AvailableImageFormats.Contains(_ImageFormat);

#endif
    }
}
