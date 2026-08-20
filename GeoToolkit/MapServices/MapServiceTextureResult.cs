// Author: František Holubec
// Created: 01.06.2026

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.GeoToolkit.Utils;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.GeoToolkit.MapServices
{
    public class MapServiceTextureResult
    {
        private readonly Part[,] _parts;
        private readonly int2 _size;
        private readonly int2 _sizeLimit;

        internal MapServiceTextureResult(Part[,] parts, int2 size, int2 sizeLimit)
        {
            _parts = parts;
            _size = size;
            _sizeLimit = sizeLimit;
        }

        public Texture2D GetTexture()
        {
            var resultTexture = new Texture2D(_size.x, _size.y);

            var xLen = _parts.GetLength(0);
            var yLen = _parts.GetLength(1);
            for (var x = 0; x < xLen; x++)
            {
                for (var y = 0; y < yLen; y++)
                {
                    var partData = _parts[x, y];
                    var partTexture = new Texture2D(2, 2);
                    try
                    {
                        // LoadImage handles PNG and JPEG only - a TIFF request has to go through GetGrayScale2DArray instead.
                        if (!partTexture.LoadImage(partData.Data))
                            throw new InvalidOperationException($"Could not decode image part [{x},{y}] ({partData.Data?.Length ?? 0} bytes) as PNG or JPEG.");

                        if (partTexture.width != partData.Dimensions.x || partTexture.height != partData.Dimensions.y)
                            throw new InvalidOperationException($"Image part [{x},{y}] is {partTexture.width}x{partTexture.height}, expected {partData.Dimensions.x}x{partData.Dimensions.y}.");

                        var pixels = partTexture.GetPixels(0, 0, partData.Dimensions.x, partData.Dimensions.y);
                        resultTexture.SetPixels(_sizeLimit.x * x, _sizeLimit.y * y, partData.Dimensions.x, partData.Dimensions.y, pixels);
                    }
                    finally
                    {
                        if (Application.isPlaying)
                            Object.Destroy(partTexture);
                        else
                            Object.DestroyImmediate(partTexture);
                    }
                }
            }

            resultTexture.Apply();
            return resultTexture;
        }

        public double[,] GetGrayScale2DArray()
        {
            var resultArray = new double[_size.x, _size.y];

            var xLen = _parts.GetLength(0);
            var yLen = _parts.GetLength(1);
            for (var x = 0; x < xLen; x++)
            {
                for (var y = 0; y < yLen; y++)
                {
                    var partData = _parts[x, y];
                    var partArray = GeoImageUtility.LoadGrayScale(partData.Data);

                    // TIFFs come back [north, east], so accept the transposed order too.
                    var partWidth = partArray.GetLength(0);
                    var partHeight = partArray.GetLength(1);
                    if ((partWidth != partData.Dimensions.x || partHeight != partData.Dimensions.y) &&
                        (partWidth != partData.Dimensions.y || partHeight != partData.Dimensions.x))
                        throw new InvalidOperationException($"Image part [{x},{y}] is {partWidth}x{partHeight}, expected {partData.Dimensions.x}x{partData.Dimensions.y}.");

                    for (var xP = 0; xP < partData.Dimensions.x; xP++)
                    {
                        for (var yP = 0; yP < partData.Dimensions.y; yP++)
                        {
                            resultArray[_sizeLimit.x * x + xP, _sizeLimit.y * y + yP] = partArray[xP, yP];
                        }
                    }
                }
            }

            return resultArray;
        }

        // Parts are written exactly as they came from the service, one file per part, extension by content.
        public async UniTask SaveAsync(string directoryPath, string baseFileName, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(directoryPath);

            var xLen = _parts.GetLength(0);
            var yLen = _parts.GetLength(1);
            var singlePart = xLen == 1 && yLen == 1;

            var targetPaths = new List<string>();
            for (var x = 0; x < xLen; x++)
            {
                for (var y = 0; y < yLen; y++)
                {
                    var extension = _parts[x, y].Data.GetImageFormat() ?? "bin";
                    targetPaths.Add(Path.Combine(directoryPath, GetPartFileName(baseFileName, new int2(x, y), singlePart, extension)));
                }
            }

            DeleteStaleFiles(directoryPath, baseFileName, targetPaths);

            var index = 0;
            for (var x = 0; x < xLen; x++)
            {
                for (var y = 0; y < yLen; y++)
                {
                    await File.WriteAllBytesAsync(targetPaths[index++], _parts[x, y].Data, cancellationToken);
                }
            }
        }

        public static MapServiceTextureResult Load(string directoryPath, string baseFileName, int2 size, int2 sizeLimit)
        {
            var partGrid = GetPartGrid(size, sizeLimit);
            var xLen = partGrid.GetLength(0);
            var yLen = partGrid.GetLength(1);
            var singlePart = xLen == 1 && yLen == 1;

            var parts = new Part[xLen, yLen];
            for (var x = 0; x < xLen; x++)
            {
                for (var y = 0; y < yLen; y++)
                {
                    var pattern = GetPartFileName(baseFileName, new int2(x, y), singlePart, "*");
                    var filePath = FindFiles(directoryPath, pattern).FirstOrDefault();
                    if (filePath == null)
                        throw new FileNotFoundException($"No file matching '{pattern}' found in '{directoryPath}'.");

                    parts[x, y] = new Part(File.ReadAllBytes(filePath), partGrid[x, y]);
                }
            }

            return new MapServiceTextureResult(parts, size, sizeLimit);
        }

        public static bool Exists(string directoryPath, string baseFileName) =>
            FindFiles(directoryPath, $"{baseFileName}.*").Any() || FindFiles(directoryPath, $"{baseFileName}_p*.*").Any();

        internal static int2[,] GetPartGrid(int2 size, int2 sizeLimit)
        {
            var isLimited = sizeLimit.x != 0 && sizeLimit.y != 0;
            if (!isLimited || (size.x <= sizeLimit.x && size.y <= sizeLimit.y))
                return new[,] { { size } };

            var gridSize = new int2((int) math.ceil((float) size.x / sizeLimit.x), (int) math.ceil((float) size.y / sizeLimit.y));
            var grid = new int2[gridSize.x, gridSize.y];
            for (var x = 0; x < gridSize.x; x++)
            {
                var xDim = x < gridSize.x - 1 ? sizeLimit.x : size.x % sizeLimit.x;
                if (xDim == 0) xDim = sizeLimit.x;

                for (var y = 0; y < gridSize.y; y++)
                {
                    var yDim = y < gridSize.y - 1 ? sizeLimit.y : size.y % sizeLimit.y;
                    if (yDim == 0) yDim = sizeLimit.y;

                    grid[x, y] = new int2(xDim, yDim);
                }
            }
            return grid;
        }

        private static string GetPartFileName(string baseFileName, int2 gridPosition, bool singlePart, string extension) =>
            singlePart ? $"{baseFileName}.{extension}" : $"{baseFileName}_p{gridPosition.x}_{gridPosition.y}.{extension}";

        private static IEnumerable<string> FindFiles(string directoryPath, string pattern) =>
            Directory.Exists(directoryPath)
                ? Directory.GetFiles(directoryPath, pattern)
                    .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f)
                : Enumerable.Empty<string>();

        private static void DeleteStaleFiles(string directoryPath, string baseFileName, List<string> keepPaths)
        {
            var keep = new HashSet<string>(keepPaths.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
            foreach (var pattern in new[] { $"{baseFileName}.*", $"{baseFileName}_p*.*" })
            {
                foreach (var file in Directory.GetFiles(directoryPath, pattern))
                {
                    var fullPath = Path.GetFullPath(file);
                    var keptMeta = fullPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) && keep.Contains(fullPath[..^".meta".Length]);
                    if (!keep.Contains(fullPath) && !keptMeta)
                        File.Delete(file);
                }
            }
        }

        internal readonly struct Part
        {
            public byte[] Data { get; }
            public int2 Dimensions { get; }

            public Part(byte[] data, int2 dimensions)
            {
                Data = data;
                Dimensions = dimensions;
            }
        }
    }
}
