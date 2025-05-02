using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProtoGIS.Scripts.Utils
{
    [Serializable]
    public class Serializable2DArray<T> : IEnumerable<T>
    {
        [FormerlySerializedAs("size")]
        [SerializeField]
        private Vector2Int _Size;

        [FormerlySerializedAs("array")]
        [SerializeField]
        [ListDrawerSettings(OnBeginListElementGUI = "DisplayElementLabel")]
        private List<T> _Array;

        public int Width => _Size.x;
        public int Height => _Size.y;
        public Vector2Int Size => _Size;
        
        public int Count => _Array.Count;

        public T this[int x, int y]
        {
            get => _Array[x * Height + y];
            set => _Array[x * Height + y] = value;
        }
        
        public T this[int i]
        {
            get => _Array[i];
            set => _Array[i] = value;
        }
        
        public Serializable2DArray()
        {
            _Array = new List<T>(0);
            _Size = Vector2Int.zero;
        }

        public Serializable2DArray(List<T> array, int width)
        {
            _Array = array;
            _Size = new Vector2Int(width, Mathf.CeilToInt(array.Count / (float) width));
        }

        public Serializable2DArray(T[] array, int width)
        {
            _Array = array.ToList();
            _Size = new Vector2Int(width, Mathf.CeilToInt(array.Length / (float) width));
        }

        public Serializable2DArray(T[,] array)
        {
            _Size = new Vector2Int(array.GetLength(0), array.GetLength(1));
            _Array = new T[Width * Height].ToList();
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    this[x, y] = array[x, y];
                }
            }
        }

        public Serializable2DArray(List<List<T>> array)
        {
            var xSize = array.Count;
            var ySize = array.Max(t => t.Count);

            _Size = new Vector2Int(xSize, ySize);
            _Array = new T[Width * Height].ToList();

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    try
                    {
                        this[x, y] = array[x][y]; 
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        public Serializable2DArray(int width, int height)
        {
            _Size = new Vector2Int(width, height);
            _Array = new T[Width * Height].ToList();
        }

        public Serializable2DArray(Vector2Int size)
        {
            _Size = size;
            _Array = new T[Width * Height].ToList();
        }
        
        public bool HasIndex(Vector2Int pos)
        {
            return pos.x < Width && pos.y < Height;
        }
        
        public bool HasIndex(int x, int y)
        {
            return x < Width && y < Height;
        }
        
        public T GetElement(Vector2Int pos)
        {
            return this[pos.x, pos.y];
        }
        
        public bool TryGetElement(Vector2Int pos, out T element)
        {
            return TryGetElement(pos.x, pos.y, out element);
        }
        
        public bool TryGetElement(int xPos, int yPos, out T element)
        {
            element = default;
            if (!HasIndex(xPos, yPos)) return false;
            
            element = this[xPos, yPos];
            return true;
        }
        
        public T[] GetCol(int x)
        {
            if (x < 0 || x >= Width) return null;

            var output = new T[Height];
            for (var y = 0; y < Height; y++)
            {
                output[y] = this[x, y];
            }

            return output;
        }

        public T[] GetRow(int y)
        {
            if (y < 0 || y >= Height) return null;

            var output = new T[Width];
            for (var x = 0; x < Width; x++)
            {
                output[x] = this[x, y];
            }

            return output;
        }
        
        public void SetCol(int x, T[] col)
        {
            if (x < 0 || x >= Width) return;
            
            var height = Mathf.Min(col.Length, Height);
            for (var y = 0; y < height; y++)
            {
                this[x, y] = col[y];
            }
        }

        public void SetRow(int y, T[] row)
        {
            if (y < 0 || y >= Height) return;
            
            var width = Mathf.Min(row.Length, Width);
            for (var x = 0; x < width; x++)
            {
                this[x, y] = row[x];
            }
        }

        public T[,] Get2DArray()
        {
            var output = new T[Width, Height];
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    output[x, y] = this[x, y];
                }
            }

            return output;
        }

        public T[] GetArray()
        {
            return _Array.ToArray();
        }
        
        public List<T> GetList()
        {
            return _Array;
        }


        public void Clear()
        {
            _Array.Clear();
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void DisplayElementLabel(int index)
        {
            var x = index / Height;
            var y = index % Height;
            GUILayout.Label($"Element [{x};{y}]");
        }
#endif
        public IEnumerator<T> GetEnumerator()
        {
            return _Array.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Array.GetEnumerator();
        }
    }
}