// Author: František Holubec
// Created: 03.02.2026

using PurrNet.Packing;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EDIVE.Networking.Serialization
{
    public static class Texture2DSerializer
    {
        public static void Write(this BitPacker packer, Texture2D value)
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
            packer.Write((int)value.filterMode);
            packer.Write((int)value.wrapMode);

            var data = value.GetRawTextureData();
            Packer<byte[]>.Write(packer, data);
        }

        public static void Read(this BitPacker packer, ref Texture2D value)
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
            var filterModeInt = 0;
            var wrapModeInt = 0;
            byte[] data = null;

            packer.Read(ref width);
            packer.Read(ref height);
            packer.Read(ref formatInt);
            packer.Read(ref filterModeInt);
            packer.Read(ref wrapModeInt);
            Packer<byte[]>.Read(packer, ref data);

            value = new Texture2D(width, height, (GraphicsFormat)formatInt, TextureCreationFlags.None)
            {
                filterMode = (FilterMode)filterModeInt,
                wrapMode = (TextureWrapMode)wrapModeInt
            };
            value.LoadRawTextureData(data);
            value.Apply();
        }
    }
}
