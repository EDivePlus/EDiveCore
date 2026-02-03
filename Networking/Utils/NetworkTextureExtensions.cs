// Author: František Holubec
// Created: 03.02.2026

using FishNet.Serializing;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EDIVE.Networking.Utils
{
    public static class NetworkTextureExtensions
    {
        public static void WriteRenderTexture(this Writer writer, RenderTexture value)
        {
            if (value == null)
            {
                writer.WriteBoolean(false);
                return;
            }
            
            writer.WriteBoolean(true);
            writer.WriteInt32(value.width);
            writer.WriteInt32(value.height);
            writer.WriteInt32((int)value.graphicsFormat);
     
            var temp = new Texture2D(value.width, value.height, value.graphicsFormat, TextureCreationFlags.None);
            var prev = RenderTexture.active;
            RenderTexture.active = value;
            temp.ReadPixels(new Rect(0, 0, value.width, value.height), 0, 0);
            temp.Apply();
            RenderTexture.active = prev;
            
            var data = temp.GetRawTextureData();
            writer.WriteUInt8ArrayAndSize(data);
            Object.Destroy(temp);
        }
        
        public static RenderTexture ReadRenderTexture(this Reader reader)
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
            return rt;
        }
        
        public static void WriteTexture2D(this Writer writer, Texture2D value)
        {
            if (value == null)
            {
                writer.WriteBoolean(false);
                return;
            }
            
            writer.WriteBoolean(true);
            writer.WriteInt32(value.width);
            writer.WriteInt32(value.height);
            writer.WriteInt32((int)value.graphicsFormat);
            writer.WriteInt32((int)value.filterMode);
            writer.WriteInt32((int)value.wrapMode);
            
            var data = value.GetRawTextureData();
            writer.WriteUInt8ArrayAndSize(data);
        }
        
        public static Texture2D ReadTexture2D(this Reader reader)
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
            
            return tex;
        }
    }
}
