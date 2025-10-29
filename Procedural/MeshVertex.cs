using UnityEngine;

namespace EDIVE.Procedural
{
    public struct MeshVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;

        public MeshVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            UV = uv;
        }

        public MeshVertex(Vector3 position, Vector3 normal) : this(position, normal, Vector2.zero) { }
    }
}
