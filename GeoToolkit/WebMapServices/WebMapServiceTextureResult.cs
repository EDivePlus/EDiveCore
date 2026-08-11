// Author: František Holubec
// Created: 01.06.2026

using System;
using EDIVE.GeoToolkit.Utils;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.GeoToolkit.WebMapServices
{
    public class WebMapServiceTextureResult
    {
        private readonly Part[,] _parts;
        private readonly int2 _size;
        private readonly int2 _sizeLimit;

        internal WebMapServiceTextureResult(Part[,] parts, int2 size, int2 sizeLimit)
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
