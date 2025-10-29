using System;
using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Procedural
{
    [Serializable]
    public class ModifiedMesh : IEquatable<ModifiedMesh>
    {
        [SerializeField]
        private Mesh _Mesh;
        
        [SerializeField]
        private Vector3 _Position;
        
        [SerializeField]
        private Quaternion _Rotation;
        
        [SerializeField]
        private Vector3 _Scale = Vector3.one;
        
        public Mesh Mesh => _Mesh;
        public Vector3 Position => _Position;
        public Quaternion Rotation => _Rotation;
        public Vector3 Scale => _Scale;
        
        [NonSerialized]
        private List<MeshVertex> _vertices;
        [NonSerialized]
        private int[] _triangles;
        [NonSerialized]
        private float _minZ;
        [NonSerialized]
        private float _length;

        [NonSerialized]
        private int? _prevHash;
        
        public List<MeshVertex> Vertices
        {
            get
            {
                RecalculateIfNeeded();
                return _vertices;
            }
        }
     
        public int[] Triangles
        {
            get
            {
                RecalculateIfNeeded();
                return _triangles;
            }
        }
        
        public float MinZ
        {
            get
            {
                RecalculateIfNeeded();
                return _minZ;
            }
        }
        
        public float Length
        {
            get
            {
                RecalculateIfNeeded();
                return _length;
            }
        }

        public ModifiedMesh(Mesh mesh)
        {
            _Mesh = mesh;
        }

        public ModifiedMesh(ModifiedMesh other)
        {
            _Mesh = other._Mesh;
            _Position = other._Position;
            _Rotation = other._Rotation;
            _Scale = other._Scale;
        }

        public void RecalculateIfNeeded()
        {
            var newHash = GetHashCode();
            if (_prevHash != newHash)
            {
                _prevHash = newHash;
                Recalculate();
            }
        }
        
        public void Recalculate()
        {
            // if the mesh is reversed by scale, we must change the culling of the faces by inversing all triangles.
            // the mesh is reverse only if the number of resersing axes is impair.
            var reversed = _Scale.x < 0;
            if (_Scale.y < 0) reversed = !reversed;
            if (_Scale.z < 0) reversed = !reversed;
            _triangles = reversed ? MeshUtility.GetReversedTriangles(_Mesh) : _Mesh.triangles;

            // we transform the source mesh vertices according to rotation/translation/scale
            var i = 0;
            _vertices = new List<MeshVertex>(_Mesh.vertexCount);
            foreach (var vert in _Mesh.vertices)
            {
                var transformed = new MeshVertex(vert, _Mesh.normals[i++]);
                if (_Rotation != Quaternion.identity)
                {
                    transformed.Position = _Rotation * transformed.Position;
                    transformed.Normal = _Rotation * transformed.Normal;
                }

                if (_Scale != Vector3.one)
                {
                    transformed.Position = Vector3.Scale(transformed.Position, _Scale);
                    transformed.Normal = Vector3.Scale(transformed.Normal, _Scale);
                }

                if (_Position != Vector3.zero)
                {
                    transformed.Position += _Position;
                }

                _vertices.Add(transformed);
            }

            // find the bounds along x
            _minZ = float.MaxValue;
            var maxZ = float.MinValue;
            foreach (var vert in _vertices)
            {
                var p = vert.Position;
                maxZ = Math.Max(maxZ, p.z);
                _minZ = Math.Min(_minZ, p.z);
            }

            _length = Math.Abs(maxZ - _minZ);
        }

        public bool Equals(ModifiedMesh other)
        {
            return _Position.Equals(other._Position) && 
                   _Rotation.Equals(other._Rotation) && 
                   _Scale.Equals(other._Scale) && 
                   Equals(_Mesh, other._Mesh);
        }

        public override bool Equals(object obj)
        {
            return obj is ModifiedMesh other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_Position, _Rotation, _Scale, _Mesh);
        }
    }
}
