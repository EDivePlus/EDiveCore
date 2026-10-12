// Author: František Holubec
// Created: 12.10.2026

using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.UIElements.SDFShapes
{
    // One row per distinct gradient in a texture every SDF graphic uses as main texture, so they all still batch.
    // Graphics with the same stops share a row. Unused rows stay baked until the space is needed.
    internal static class SDFGradientAtlas
    {
        public const int WIDTH = 512;
        private const int START_ROWS = 32;

        private static Texture2D _texture;
        private static Color32[] _pixels;
        private static Color32[] _rowBuffer;
        private static Row[] _rows = new Row[START_ROWS];
        private static readonly Dictionary<int, List<Row>> ROWS_BY_HASH = new();
        private static int _releaseCounter;

        public static Texture2D Texture
        {
            get
            {
                if (!_texture)
                    CreateTexture(_rows.Length);
                return _texture;
            }
        }

#if UNITY_EDITOR
        // The texture outlives a script reload, the reference to it does not
        [UnityEditor.InitializeOnLoadMethod]
        private static void DestroyOnReload()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                if (_texture)
                    Object.DestroyImmediate(_texture);
            };
        }
#endif

        // Points the row at the gradient, sharing or baking one; null releases it
        public static void Assign(Gradient gradient, ref int row)
        {
            if (gradient == null)
            {
                Release(ref row);
                return;
            }

            var stops = new Stops(gradient);
            if (IsValid(row) && _rows[row].Stops.Equals(stops))
                return;

            Release(ref row);
            row = Acquire(stops, gradient);
        }

        public static void Release(ref int row)
        {
            if (IsValid(row) && --_rows[row].RefCount == 0)
                _rows[row].ReleasedAt = ++_releaseCounter;
            row = -1;
        }

        // Rows reset with a script reload while graphics may still hold an index
        private static bool IsValid(int row) => row >= 0 && row < _rows.Length && _rows[row] != null && _rows[row].RefCount > 0;

        private static int Acquire(in Stops stops, Gradient gradient)
        {
            var hash = stops.GetHashCode();
            if (ROWS_BY_HASH.TryGetValue(hash, out var sameHash))
            {
                foreach (var existing in sameHash)
                {
                    if (!existing.Stops.Equals(stops))
                        continue;
                    existing.RefCount++;
                    return existing.Index;
                }
            }

            var index = FindFreeIndex();
            var row = new Row(index, stops) { RefCount = 1 };
            _rows[index] = row;
            if (!ROWS_BY_HASH.TryGetValue(hash, out sameHash))
                ROWS_BY_HASH[hash] = sameHash = new List<Row>();
            sameHash.Add(row);
            Bake(index, gradient);
            return index;
        }

        // An empty row, else the longest unused one, else the texture doubles
        private static int FindFreeIndex()
        {
            var evict = -1;
            for (var i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                if (row == null)
                    return i;
                if (row.RefCount == 0 && (evict < 0 || row.ReleasedAt < _rows[evict].ReleasedAt))
                    evict = i;
            }

            if (evict >= 0)
            {
                var row = _rows[evict];
                ROWS_BY_HASH[row.Stops.GetHashCode()].Remove(row);
                _rows[evict] = null;
                return evict;
            }

            var index = _rows.Length;
            Grow(_rows.Length * 2);
            return index;
        }

        private static void Grow(int rowCount)
        {
            System.Array.Resize(ref _rows, rowCount);
            if (!_texture)
            {
                CreateTexture(rowCount);
                return;
            }

            System.Array.Resize(ref _pixels, WIDTH * rowCount);
            _texture.Reinitialize(WIDTH, rowCount);
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private static void CreateTexture(int rowCount)
        {
            if (_pixels == null || _pixels.Length != WIDTH * rowCount)
                System.Array.Resize(ref _pixels, WIDTH * rowCount);

            _texture = new Texture2D(WIDTH, rowCount, TextureFormat.RGBA32, false, false)
            {
                name = "SDF Gradients",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        // Texel x holds t = x / (WIDTH - 1); the shader samples texel centers to match
        private static void Bake(int index, Gradient gradient)
        {
            var texture = Texture;
            var offset = index * WIDTH;
            for (var x = 0; x < WIDTH; x++)
                _pixels[offset + x] = gradient.Evaluate(x / (WIDTH - 1f));

            texture.SetPixels32(0, index, WIDTH, 1, GetRow(index));
            texture.Apply(false);
        }

        private static Color32[] GetRow(int index)
        {
            _rowBuffer ??= new Color32[WIDTH];
            System.Array.Copy(_pixels, index * WIDTH, _rowBuffer, 0, WIDTH);
            return _rowBuffer;
        }

        private class Row
        {
            public readonly int Index;
            public readonly Stops Stops;
            public int RefCount;
            public int ReleasedAt;

            public Row(int index, Stops stops)
            {
                Index = index;
                Stops = stops;
            }
        }

        // What decides the baked colors, compared by value
        private readonly struct Stops : System.IEquatable<Stops>
        {
            private readonly GradientColorKey[] _colorKeys;
            private readonly GradientAlphaKey[] _alphaKeys;
            private readonly GradientMode _mode;
            private readonly ColorSpace _colorSpace;

            public Stops(Gradient gradient)
            {
                _colorKeys = gradient.colorKeys;
                _alphaKeys = gradient.alphaKeys;
                _mode = gradient.mode;
                _colorSpace = gradient.colorSpace;
            }

            public bool Equals(Stops other)
            {
                if (_mode != other._mode || _colorSpace != other._colorSpace || _colorKeys.Length != other._colorKeys.Length || _alphaKeys.Length != other._alphaKeys.Length)
                    return false;
                for (var i = 0; i < _colorKeys.Length; i++)
                {
                    if (!_colorKeys[i].color.Equals(other._colorKeys[i].color) || !_colorKeys[i].time.Equals(other._colorKeys[i].time))
                        return false;
                }
                for (var i = 0; i < _alphaKeys.Length; i++)
                {
                    if (!_alphaKeys[i].alpha.Equals(other._alphaKeys[i].alpha) || !_alphaKeys[i].time.Equals(other._alphaKeys[i].time))
                        return false;
                }
                return true;
            }

            public override bool Equals(object obj) => obj is Stops other && Equals(other);

            public override int GetHashCode()
            {
                var hash = ((int) _mode * 397) ^ (int) _colorSpace;
                foreach (var key in _colorKeys)
                    hash = hash * 31 + key.color.GetHashCode() * 17 + key.time.GetHashCode();
                foreach (var key in _alphaKeys)
                    hash = hash * 31 + key.alpha.GetHashCode() * 17 + key.time.GetHashCode();
                return hash;
            }
        }
    }
}
