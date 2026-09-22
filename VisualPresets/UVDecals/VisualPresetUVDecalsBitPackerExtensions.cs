// Author: Michal Petr
// Created: 21.09.2026

#if PURRNET
using EDIVE.AssetTranslation;
using EDIVE.Rendering.UVDecals;
using JetBrains.Annotations;
using PurrNet.Packing;
using UnityEngine;

namespace EDIVE.VisualPresets.UVDecals
{
    [UsedImplicitly]
    public static class VisualPresetUVDecalsBitPackerExtensions
    {
        public static void Write(this BitPacker packer, UVDecalVisualPresetRecord value)
        {
            packer.CustomWriteTranslatedDefinition(value?.VisualID);
            packer.Write(value?.Decal);
        }

        public static void Read(this BitPacker packer, ref UVDecalVisualPresetRecord value)
        {
            var id = packer.CustomReadTranslatedDefinition<UVDecalVisualID>();
            UVDecalPreset decal = null;
            packer.Read(ref decal);
            value = new UVDecalVisualPresetRecord(id, decal);
        }

        public static void Write(this BitPacker packer, UVDecalPreset value)
        {
            var hasValue = value != null;
            packer.WriteBit(hasValue);
            if (!hasValue)
                return;

            Packer.WriteAsNetworkAsset(packer, value._Placement);
            Packer.WriteAsNetworkAsset(packer, value._Texture);
            Packer<Color>.Write(packer, value._Tint);
            packer.WriteBit(value._OverrideSmoothness);
            Packer<float>.Write(packer, value._Smoothness);
            Packer<Vector2>.Write(packer, value._Scale);
            Packer<Vector2>.Write(packer, value._Anchor);
            Packer<Vector2>.Write(packer, value._Pivot);
            Packer<float>.Write(packer, value._Rotation);
        }

        public static void Read(this BitPacker packer, ref UVDecalPreset value)
        {
            if (!packer.ReadBit())
            {
                value = null;
                return;
            }

            UVDecalPlacement placement = null;
            Packer.ReadAsNetworkAsset(packer, ref placement);

            Texture2D texture = null;
            Packer.ReadAsNetworkAsset(packer, ref texture);

            Color tint = default;
            Packer<Color>.Read(packer, ref tint);

            var overrideSmoothness = packer.ReadBit();

            float smoothness = 0;
            Packer<float>.Read(packer, ref smoothness);

            Vector2 scale = default;
            Packer<Vector2>.Read(packer, ref scale);

            Vector2 anchor = default;
            Packer<Vector2>.Read(packer, ref anchor);

            Vector2 pivot = default;
            Packer<Vector2>.Read(packer, ref pivot);

            float rotation = 0;
            Packer<float>.Read(packer, ref rotation);

            value = new UVDecalPreset
            {
                _Placement = placement,
                _Texture = texture,
                _Tint = tint,
                _OverrideSmoothness = overrideSmoothness,
                _Smoothness = smoothness,
                _Scale = scale,
                _Anchor = anchor,
                _Pivot = pivot,
                _Rotation = rotation
            };
        }
    }
}
#endif
