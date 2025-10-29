using System;
using UnityEngine;

namespace EDIVE.Procedural.SplineMesh
{
    public struct CurveSample : IEquatable<CurveSample>
    {
        public readonly Vector3 Location;
        public readonly Vector3 Tangent;
        public readonly Vector3 Up;

        private Quaternion? _rotation;
        public Quaternion Rotation => _rotation ??= Quaternion.LookRotation(Tangent, Up);

        public CurveSample(Vector3 location, Vector3 tangent, Vector3 up) : this()
        {
            Location = location;
            Tangent = tangent;
            Up = up;
        }

        public MeshVertex Bend(MeshVertex vert)
        {
            var res = new MeshVertex(vert.Position, vert.Normal, vert.UV);
            res.Position.z = 0;
            res.Position = Rotation * res.Position + Location;
            res.Normal = Rotation * res.Normal;
            return res;
        }
        
        public bool Equals(CurveSample other)
        {
            return Location.Equals(other.Location) && 
                   Tangent.Equals(other.Tangent) && 
                   Up.Equals(other.Up);
        }

        public override bool Equals(object obj)
        {
            return obj is CurveSample other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Location, Tangent, Up);
        }
    }
}
