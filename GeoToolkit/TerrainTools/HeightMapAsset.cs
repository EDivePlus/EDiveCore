using EDIVE.DataStructures;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.GeoToolkit.TerrainTools
{
    public class HeightMapAsset : ScriptableObject
    {
        [FormerlySerializedAs("data")]
        [SerializeField]
        private Serializable2DArray<Serializable2DArray<float>> _Data = new();

        public Serializable2DArray<Serializable2DArray<float>> Data
        {
            get => _Data;
            set => _Data = value;
        }

        public bool TryGetTile(int x, int y, out Serializable2DArray<float> tile)
        {
            tile = null;
            if (x < 0 || x >= _Data.Width || y < 0 || y >= _Data.Height)
                return false;
            tile = _Data[x, y];
            return tile != null;
        }

        public void FixTerrainHeightGaps()
        {
            for (var x = 0; x < _Data.Width; x++)
            {
                for (var y = 0; y < _Data.Height; y++)
                {
                    var current = _Data[x, y];
                    if (current == null) continue;

                    if (TryGetTile(x + 1, y, out var top))
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