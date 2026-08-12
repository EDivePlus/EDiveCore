using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if LIBTIFF_NET
using BitMiracle.LibTiff.Classic;
#endif

namespace EDIVE.GeoToolkit.Utils
{
    public static class GeoImageUtility
    {
        public static float[,] LoadGrayScale(this Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;
            var heights = new float[width, height];
            var pixels = texture.GetPixels();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixel = pixels[y * width + x];
                    heights[x, y] = pixel.r;
                }
            }
            return heights;
        }

        public static double[,] LoadGrayScaleDouble(this Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;
            var heights = new double[width, height];
            var pixels = texture.GetPixels();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixel = pixels[y * width + x];
                    heights[x, y] = pixel.r;
                }
            }
            return heights;
        }

        public static double[,] LoadGrayScale(byte[] data)
        {
            var imageFormat = data.GetImageFormat();
            if (string.IsNullOrEmpty(imageFormat))
                throw new NotSupportedException("Unrecognised image format");
            
            return imageFormat switch
            {
                "png" or "jpg" or "bmp" or "gif" => LoadTextureGrayScale(data),
#if LIBTIFF_NET
                "tif" => LoadTiffGrayScale(data),
#else
                "tif" => throw new NotSupportedException("Tiff not supported without LibTiff.net, istall using Nuget and enable LIBTIFF_NET define"),
#endif
                _ => throw new NotSupportedException($"{imageFormat} is not a supported image format for grayscale loading.")
            };
        }

        // Only 8 bits per channel, so usable for imagery but not for elevation.
        private static double[,] LoadTextureGrayScale(byte[] data)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RFloat, false);
            try
            {
                if (!texture.LoadImage(data))
                    throw new InvalidOperationException($"Failed to decode a {data.Length} byte image.");
                return texture.LoadGrayScaleDouble();
            }
            finally
            {
                if (Application.isPlaying)
                    Object.Destroy(texture);
                else
                    Object.DestroyImmediate(texture);
            }
        }

#if LIBTIFF_NET
        private static double[,] LoadTiffGrayScale(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var tiff = Tiff.ClientOpen("in-memory", "r", stream, new TiffStream());
            if (tiff == null)
                throw new InvalidOperationException("Could not open the data as a TIFF.");

            var width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            var height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            var bitsPerSample = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            var samplesPerPixel = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            var sampleFormatField = tiff.GetField(TiffTag.SAMPLEFORMAT);
            var sampleFormat = sampleFormatField == null ? SampleFormat.UINT : (SampleFormat) sampleFormatField[0].ToInt();

            if (samplesPerPixel != 1)
                throw new NotSupportedException($"Expected a single band raster, the TIFF has {samplesPerPixel}.");

            // Indexed [north, east] to match what TerrainData.SetHeights expects, so rows are flipped as TIFF runs top down.
            var result = new double[height, width];
            if (tiff.IsTiled())
                ReadTiles(tiff, result, width, height, bitsPerSample, sampleFormat);
            else
                ReadScanLines(tiff, result, width, height, bitsPerSample, sampleFormat);

            return result;
        }

        private static void ReadTiles(Tiff tiff, double[,] result, int width, int height, int bitsPerSample, SampleFormat sampleFormat)
        {
            var tileWidth = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            var tileHeight = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();
            var buffer = new byte[tiff.TileSize()];

            for (var originY = 0; originY < height; originY += tileHeight)
            {
                for (var originX = 0; originX < width; originX += tileWidth)
                {
                    if (tiff.ReadTile(buffer, 0, originX, originY, 0, 0) < 0)
                        throw new InvalidOperationException($"Failed to read the TIFF tile at [{originX},{originY}].");

                    for (var y = 0; y < tileHeight && originY + y < height; y++)
                    {
                        for (var x = 0; x < tileWidth && originX + x < width; x++)
                        {
                            result[height - 1 - (originY + y), originX + x] = ReadSample(buffer, y * tileWidth + x, bitsPerSample, sampleFormat);
                        }
                    }
                }
            }
        }

        private static void ReadScanLines(Tiff tiff, double[,] result, int width, int height, int bitsPerSample, SampleFormat sampleFormat)
        {
            var buffer = new byte[tiff.ScanlineSize()];
            for (var y = 0; y < height; y++)
            {
                if (!tiff.ReadScanline(buffer, y))
                    throw new InvalidOperationException($"Failed to read TIFF scanline {y}.");

                for (var x = 0; x < width; x++)
                {
                    result[height - 1 - y, x] = ReadSample(buffer, x, bitsPerSample, sampleFormat);
                }
            }
        }

        private static double ReadSample(byte[] buffer, int index, int bitsPerSample, SampleFormat sampleFormat)
        {
            return (bitsPerSample, sampleFormat) switch
            {
                (32, SampleFormat.IEEEFP) => BitConverter.ToSingle(buffer, index * 4),
                (64, SampleFormat.IEEEFP) => BitConverter.ToDouble(buffer, index * 8),
                (32, SampleFormat.INT) => BitConverter.ToInt32(buffer, index * 4),
                (32, SampleFormat.UINT) => BitConverter.ToUInt32(buffer, index * 4),
                (16, SampleFormat.INT) => BitConverter.ToInt16(buffer, index * 2),
                (16, SampleFormat.UINT) => BitConverter.ToUInt16(buffer, index * 2),
                (8, _) => buffer[index],
                _ => throw new NotSupportedException($"Unsupported TIFF sample layout: {bitsPerSample}-bit {sampleFormat}.")
            };
        }
#endif

        public static string GetImageFormat(this byte[] imageBytes)
        {
            if (imageBytes == null) return string.Empty;
            foreach(var imageType in IMAGE_FORMAT_DECODERS)
            {
                if (imageType.Key.SequenceEqual(imageBytes.Take(imageType.Key.Length)))
                    return imageType.Value;
            }
            return null;
        }

        private static readonly Dictionary<byte[], string> IMAGE_FORMAT_DECODERS = new()
        {
            { new byte[]{ 66, 77 }, "bmp"},
            { new byte[]{ 71, 73, 70 }, "gif" },
            { new byte[]{ 73, 73, 42 }, "tif" },
            { new byte[]{ 77, 77, 42 }, "tif" },
            { new byte[]{ 137, 80, 78, 71 }, "png" },
            { new byte[]{ 255, 216, 255, 224 }, "jpg" },
            { new byte[]{ 255, 216, 255, 225 }, "jpg" },
            { new byte[]{ 0x52, 0x49, 0x46, 0x46 }, "webp" },
            { new byte[]{ 0x3C, 0x3F, 0x78, 0x6D, 0x6C, 0x20 }, "xml" },
        };
    }
}