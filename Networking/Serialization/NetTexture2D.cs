// Author: František Holubec
// Created: 03.02.2026

using FishNet.CodeGenerating;
using FishNet.Serializing;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EDIVE.Networking.Serialization
{
    [UseGlobalCustomSerializer]
    public class NetTexture2D
    {
        public Texture2D Value { get; }

        public NetTexture2D() { }
        public NetTexture2D(Texture2D value)
        {
            Value = value;
        }

        public static implicit operator Texture2D(NetTexture2D wrapper)
        {
            return wrapper?.Value;
        }

        public static implicit operator NetTexture2D(Texture2D value)
        {
            return value == null ? null : new NetTexture2D(value);
        }
    }
    
    public static class NetTexture2DExtensions
    {
        public static void WriteNetTexture2D(this Writer writer, NetTexture2D value)
        {
            var tex = value?.Value;
            if (tex == null)
            {
                writer.WriteBoolean(false);
                return;
            }
            
            writer.WriteBoolean(true);
            writer.WriteInt32(tex.width);
            writer.WriteInt32(tex.height);
            writer.WriteInt32((int)tex.graphicsFormat);
            writer.WriteInt32((int)tex.filterMode);
            writer.WriteInt32((int)tex.wrapMode);
            
            var data = tex.GetRawTextureData();
            writer.WriteUInt8ArrayAndSize(data);
        }
        
        public static NetTexture2D ReadNetTexture2D(this Reader reader)
        {
            var hasValue = reader.ReadBoolean();
            if (!hasValue)
                return null;
            
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var format = (GraphicsFormat)reader.ReadInt32();
            var filterMode = (FilterMode)reader.ReadInt32();
            var wrapMode = (TextureWrapMode)reader.ReadInt32();
            var data = reader.ReadUInt8ArrayAndSizeAllocated();
            
            var tex = new Texture2D(width, height, format, TextureCreationFlags.None)
            {
                filterMode = filterMode,
                wrapMode = wrapMode
            };
            tex.LoadRawTextureData(data);
            tex.Apply();
            
            return new NetTexture2D(tex);
        }
    }
}
