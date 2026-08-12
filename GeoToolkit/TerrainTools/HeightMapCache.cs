// Author: František Holubec
// Created: 12.08.2026

using System;
using System.IO;
using EDIVE.DataStructures;
using UnityEngine;

namespace EDIVE.GeoToolkit.TerrainTools
{
    // Raw download cache for the generator, kept out of the asset pipeline: the folder name ends in '~' so Unity skips it.
    public class HeightMapCache
    {
        private const string DIRECTORY_NAME = "HeightMapCache~";
        private const string FILE_NAME = "HeightMap.bin";
        private const int MAGIC = 0x4D484445;
        private const int VERSION = 1;

        private Serializable2DArray<Serializable2DArray<float>> _data = new();

        public Serializable2DArray<Serializable2DArray<float>> Data
        {
            get => _data;
            set => _data = value ?? new Serializable2DArray<Serializable2DArray<float>>();
        }

        public static string GetDirectoryPath(string assetsRelativeFolder) =>
            Path.GetFullPath(Path.Combine(assetsRelativeFolder, DIRECTORY_NAME));

        public static string GetFilePath(string assetsRelativeFolder) =>
            Path.Combine(GetDirectoryPath(assetsRelativeFolder), FILE_NAME);

        public static HeightMapCache Load(string assetsRelativeFolder)
        {
            var filePath = GetFilePath(assetsRelativeFolder);
            if (!File.Exists(filePath))
                return new HeightMapCache();

            try
            {
                using var stream = File.OpenRead(filePath);
                using var reader = new BinaryReader(stream);

                if (reader.ReadInt32() != MAGIC)
                    throw new InvalidDataException($"'{filePath}' is not a height map cache.");

                var version = reader.ReadInt32();
                if (version != VERSION)
                    throw new InvalidDataException($"Height map cache is version {version}, expected {VERSION}.");

                var gridWidth = reader.ReadInt32();
                var gridHeight = reader.ReadInt32();
                var data = new Serializable2DArray<Serializable2DArray<float>>(gridWidth, gridHeight);
                for (var x = 0; x < gridWidth; x++)
                {
                    for (var y = 0; y < gridHeight; y++)
                    {
                        data[x, y] = ReadTile(reader);
                    }
                }

                return new HeightMapCache { Data = data };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not read the height map cache, treating it as empty. {e.Message}");
                return new HeightMapCache();
            }
        }

        public void Save(string assetsRelativeFolder)
        {
            Directory.CreateDirectory(GetDirectoryPath(assetsRelativeFolder));

            using var stream = File.Create(GetFilePath(assetsRelativeFolder));
            using var writer = new BinaryWriter(stream);

            writer.Write(MAGIC);
            writer.Write(VERSION);
            writer.Write(_data.Width);
            writer.Write(_data.Height);
            for (var x = 0; x < _data.Width; x++)
            {
                for (var y = 0; y < _data.Height; y++)
                {
                    WriteTile(writer, _data[x, y]);
                }
            }
        }

        private static Serializable2DArray<float> ReadTile(BinaryReader reader)
        {
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            if (width <= 0 || height <= 0)
                return null;

            var byteCount = width * height * sizeof(float);
            var bytes = reader.ReadBytes(byteCount);
            if (bytes.Length != byteCount)
                throw new EndOfStreamException("The height map cache ends in the middle of a tile.");

            var values = new float[width * height];
            Buffer.BlockCopy(bytes, 0, values, 0, byteCount);
            return new Serializable2DArray<float>(values, width);
        }

        private static void WriteTile(BinaryWriter writer, Serializable2DArray<float> tile)
        {
            if (tile == null || tile.Count == 0)
            {
                writer.Write(0);
                writer.Write(0);
                return;
            }

            writer.Write(tile.Width);
            writer.Write(tile.Height);

            var values = tile.GetArray();
            var bytes = new byte[values.Length * sizeof(float)];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            writer.Write(bytes);
        }

        public bool TryGetTile(int x, int y, out Serializable2DArray<float> tile)
        {
            tile = null;
            if (x < 0 || x >= _data.Width || y < 0 || y >= _data.Height)
                return false;
            tile = _data[x, y];
            return tile != null;
        }

        public void FixTerrainHeightGaps()
        {
            for (var x = 0; x < _data.Width; x++)
            {
                for (var y = 0; y < _data.Height; y++)
                {
                    var current = _data[x, y];
                    if (current == null) continue;

                    if (TryGetTile(x, y + 1, out var top))
                    {
                        var col = top.GetCol(0);
                        current.SetCol(current.Width - 1, col);
                    }

                    if (TryGetTile(x + 1, y, out var right))
                    {
                        var row = right.GetRow(0);
                        current.SetRow(current.Height - 1, row);
                    }

                    if (TryGetTile(x + 1, y + 1, out var topRight))
                    {
                        current[current.Width - 1, current.Height - 1] = topRight[0, 0];
                    }
                }
            }
        }
    }
}
