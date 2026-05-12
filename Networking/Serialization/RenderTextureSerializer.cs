// Author: František Holubec
// Created: 03.02.2026

using PurrNet.Packing;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EDIVE.Networking.Serialization
{
    public static class RenderTextureSerializer
    {
        public static void Write(this BitPacker packer, RenderTexture value)
        {
            if (value == null)
            {
                packer.Write(false);
                return;
            }

            packer.Write(true);
            packer.Write(value.width);
            packer.Write(value.height);
            packer.Write((int)value.graphicsFormat);

            // Round-trip the GPU texture into a CPU-readable copy so we can ship raw pixels.
            var temp = new Texture2D(value.width, value.height, value.graphicsFormat, TextureCreationFlags.None);
            var prev = RenderTexture.active;
            RenderTexture.active = value;
            temp.ReadPixels(new Rect(0, 0, value.width, value.height), 0, 0);
            temp.Apply();
            RenderTexture.active = prev;

            var data = temp.GetRawTextureData();
            Packer<byte[]>.Write(packer, data);
            Object.Destroy(temp);
        }

        public static void Read(this BitPacker packer, ref RenderTexture value)
        {
            var hasValue = false;
            packer.Read(ref hasValue);
            if (!hasValue)
            {
                value = null;
                return;
            }

            var width = 0;
            var height = 0;
            var formatInt = 0;
            byte[] data = null;

            packer.Read(ref width);
            packer.Read(ref height);
            packer.Read(ref formatInt);
            Packer<byte[]>.Read(packer, ref data);

            var format = (GraphicsFormat)formatInt;
            value = new RenderTexture(width, height, 0, format)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            value.Create();

            var temp = new Texture2D(width, height, format, TextureCreationFlags.None);
            temp.LoadRawTextureData(data);
            temp.Apply();

            Graphics.Blit(temp, value);
            Object.Destroy(temp);
        }
    }
}
