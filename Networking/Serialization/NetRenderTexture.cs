// Author: František Holubec
// Created: 03.02.2026

using FishNet.CodeGenerating;
using FishNet.Serializing;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EDIVE.Networking.Serialization
{
    [UseGlobalCustomSerializer]
    public class NetRenderTexture
    {
        public RenderTexture Value { get; }

        public NetRenderTexture() { }
        public NetRenderTexture(RenderTexture value)
        {
            Value = value;
        }

        public static implicit operator RenderTexture(NetRenderTexture wrapper)
        {
            return wrapper?.Value;
        }

        public static implicit operator NetRenderTexture(RenderTexture value)
        {
            return value == null ? null : new NetRenderTexture(value);
        }
    }
    
    public static class NetRenderTextureExtensions
    {
        public static void WriteNetRenderTexture(this Writer writer, NetRenderTexture value)
        {
            var rt = value?.Value;
            if (rt == null)
            {
                writer.WriteBoolean(false);
                return;
            }
            
            writer.WriteBoolean(true);
            writer.WriteInt32(rt.width);
            writer.WriteInt32(rt.height);
            writer.WriteInt32((int)rt.graphicsFormat);
     
            var temp = new Texture2D(rt.width, rt.height, rt.graphicsFormat, TextureCreationFlags.None);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            temp.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            temp.Apply();
            RenderTexture.active = prev;
            
            var data = temp.GetRawTextureData();
            writer.WriteUInt8ArrayAndSize(data);
            Object.Destroy(temp);
        }
        
        public static NetRenderTexture ReadNetRenderTexture(this Reader reader)
        {
            var hasValue = reader.ReadBoolean();
            if (!hasValue)
                return null;
            
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var format = (GraphicsFormat)reader.ReadInt32();
            var data = reader.ReadUInt8ArrayAndSizeAllocated();
            
            var rt = new RenderTexture(width, height, 0, format)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();
            
            var temp = new Texture2D(width, height, format, TextureCreationFlags.None);
            temp.LoadRawTextureData(data);
            temp.Apply();
            
            Graphics.Blit(temp, rt);
            Object.Destroy(temp);
            return new NetRenderTexture(rt);
        }
    }
}
