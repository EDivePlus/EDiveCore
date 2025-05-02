using System;
using System.Collections.Generic;
using System.Linq;
using OSGeo.GDAL;

namespace EDIVE.GeoToolkit
{
    public static class GeoImageUtility
    {
        public static double[,] LoadGrayScale(string filePath)
        {
            Gdal.AllRegister();
            using var dataset = Gdal.Open(filePath, Access.GA_ReadOnly);
            return LoadGrayScale(dataset);
        }
        
        public static double[,] LoadGrayScale(byte[] data)
        {
            // TODO find a better way than saving and loading again? is it even possible (in mind with 32bit TIFF)
            Gdal.AllRegister();
            var tmpPath = "/vsimem/inmem";
#if UNITY_EDITOR
            tmpPath = $"/vsimem/inmem{UnityEditor.GUID.Generate()}";
#endif
            Gdal.FileFromMemBuffer(tmpPath, data);
            var dataset = Gdal.Open(tmpPath, Access.GA_ReadOnly);
            return LoadGrayScale(dataset);
        }

        public static double[,] LoadGrayScale(Dataset dataset)
        {
            var band = dataset.GetRasterBand(1);

            var width = band.XSize;
            var height = band.YSize;
      
            var buffer = new double[width * height];
            band.ReadRaster(0, 0, width, height, buffer, width, height, 0, 0);
            
            var output = new double[width, height];
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    try
                    {
                        output[width - 1 - i, j] = buffer[i * height + j];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                }
            }
            return output;
        }

        public static string GetImageFormat(this byte[] imageBytes)
        {
            if (imageBytes == null) return string.Empty;
            foreach(var imageType in ImageFormatDecoders)
            {
                if (imageType.Key.SequenceEqual(imageBytes.Take(imageType.Key.Length)))
                    return imageType.Value;
            }
            return null;
        }

        private static readonly Dictionary<byte[], string> ImageFormatDecoders = new Dictionary<byte[], string>
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