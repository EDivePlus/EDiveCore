// Author: František Holubec
// Created: 01.06.2026

using EDIVE.GeoToolkit.Utils;
using Unity.Mathematics;
using UnityEngine;

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
                    var partTexture = new Texture2D(0, 0);
                    partTexture.LoadImage(partData.Data);
                    if (partTexture.width != partData.Dimensions.x || partTexture.height != partData.Dimensions.y)
                        partTexture.Reinitialize(partData.Dimensions.x, partData.Dimensions.y);
                    var pixels = partTexture.GetPixels(0, 0, partData.Dimensions.x, partData.Dimensions.y);
                    resultTexture.SetPixels(_sizeLimit.x * x, _sizeLimit.y * y, partData.Dimensions.x, partData.Dimensions.y, pixels);
                }
            }
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
