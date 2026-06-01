using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace EDIVE.GeoToolkit.WebMapServices
{
    [Serializable]
    public class WebMapServiceRequest
    {
        [FormerlySerializedAs("serverDefinition")]
        [SerializeField]
        [ShowCreateNew]
        [EnhancedInlineEditor]
        private WebMapServiceServerDefinition _ServerDefinition;

        [FormerlySerializedAs("imageFormat")]
        [PropertySpace]
        [SerializeField]
        [ValidateInput("IsImageFormatInvalid")]
        [ValueDropdown(nameof(AvailableImageFormats), FlattenTreeView = true)]
        private string _ImageFormat;

        [FormerlySerializedAs("coordinateSystem")]
        [SerializeField]
        [ValidateInput("IsCoordinateSystemInvalid")]
        [ValueDropdown(nameof(AvailableCoordinateSystems), FlattenTreeView = true)]
        private string _CoordinateSystem;

        [FormerlySerializedAs("layer")]
        [SerializeField]
        [ValidateInput("IsLayerInvalid")]
        [ValueDropdown(nameof(AvailableLayers), FlattenTreeView = true)]
        private string _Layer;

        private IEnumerable<string> AvailableImageFormats => _ServerDefinition ? _ServerDefinition.ImageFormats : new List<string>();
        private IEnumerable<string> AvailableCoordinateSystems => _ServerDefinition ? _ServerDefinition.CoordinateSystems : new List<string>();
        private IEnumerable<string> AvailableLayers => _ServerDefinition ? _ServerDefinition.Layers : new List<string>();

        public bool IsSizeLimited => SizeLimit.x != 0 && SizeLimit.y != 0;
        public int2 SizeLimit => _ServerDefinition ? _ServerDefinition.SizeLimit : int2.zero;

        public async UniTask<WebMapServiceTextureResult> DownloadAsync(GeoAreaRect geoArea, int2 size, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (IsSizeLimited && (size.x > SizeLimit.x || size.y > SizeLimit.y))
                return await DownloadPartWiseAsync(geoArea, size, progress, cancellationToken);

            return await DownloadAsOneAsync(geoArea, size, progress, cancellationToken);
        }

        private string GenerateURL(GeoAreaRect geoArea, int2 textureSize)
        {
            return _ServerDefinition.GenerateURL(_CoordinateSystem, _ImageFormat, _Layer, geoArea, textureSize);
        }

        private async UniTask<WebMapServiceTextureResult> DownloadPartWiseAsync(GeoAreaRect geoArea, int2 size, IProgress<float> progress, CancellationToken cancellationToken)
        {
            var subAreas = geoArea.Split(size, SizeLimit);
            var subAreasDimensions = new int2(subAreas.GetLength(0), subAreas.GetLength(1));
            var partsCount = subAreasDimensions.x * subAreasDimensions.y;

            var parts = new WebMapServiceTextureResult.Part[subAreasDimensions.x, subAreasDimensions.y];

            for (var x = 0; x < subAreasDimensions.x; x++)
            {
                var xDim = x < subAreasDimensions.x - 1 ? SizeLimit.x : size.x % SizeLimit.x;
                if (xDim == 0) xDim = SizeLimit.x;

                for (var y = 0; y < subAreasDimensions.y; y++)
                {
                    var yDim = y < subAreasDimensions.y - 1 ? SizeLimit.y : size.y % SizeLimit.y;
                    if (yDim == 0) yDim = SizeLimit.y;

                    var dimensions = new int2(xDim, yDim);
                    var partIndex = x * subAreasDimensions.y + y;
                    var url = GenerateURL(subAreas[x, y], dimensions);

                    var partProgress = progress == null
                        ? null
                        : Progress.Create<float>(p => progress.Report((partIndex + p) / partsCount));

                    var data = await DownloadAsync(url, partProgress, cancellationToken);
                    parts[x, y] = new WebMapServiceTextureResult.Part(data, dimensions);
                }
            }

            progress?.Report(1f);
            return new WebMapServiceTextureResult(parts, size, SizeLimit);
        }

        private async UniTask<WebMapServiceTextureResult> DownloadAsOneAsync(GeoAreaRect geoArea, int2 size, IProgress<float> progress, CancellationToken cancellationToken)
        {
            var url = GenerateURL(geoArea, size);
            var data = await DownloadAsync(url, progress, cancellationToken);

            var parts = new WebMapServiceTextureResult.Part[1, 1];
            parts[0, 0] = new WebMapServiceTextureResult.Part(data, size);
            return new WebMapServiceTextureResult(parts, size, SizeLimit);
        }

        private static async UniTask<byte[]> DownloadAsync(string url, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("No URL provided.", nameof(url));

            using var webRequest = UnityWebRequest.Get(url);
            await webRequest.SendWebRequest().ToUniTask(progress, cancellationToken: cancellationToken);
            return webRequest.downloadHandler.data;
        }


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
        private bool IsLayerInvalid => AvailableLayers.Contains(_Layer);

        [UsedImplicitly]
        private bool IsCoordinateSystemInvalid => AvailableCoordinateSystems.Contains(_CoordinateSystem);

        [UsedImplicitly]
        private bool IsImageFormatInvalid => AvailableImageFormats.Contains(_ImageFormat);

#endif
    }
}
