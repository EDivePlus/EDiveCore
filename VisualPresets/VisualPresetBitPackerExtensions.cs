// Author: František Holubec
// Created: 17.06.2026

#if PURRNET
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EDIVE.AssetTranslation;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using JetBrains.Annotations;
using PurrNet.Packing;
using UnityEngine;

namespace EDIVE.VisualPresets
{
    [UsedImplicitly]
    public static class VisualPresetBitPackerExtensions
    {
        public static void Write(this BitPacker packer, ABaseVisualID value) => packer.CustomWriteTranslatedDefinition(value);
        public static void Read(this BitPacker packer, ref ABaseVisualID value) => value = packer.CustomReadTranslatedDefinition<ABaseVisualID>();
        
        public static void Write(this BitPacker packer, VisualPreset value)
        {
            var records = value?.EnumerateValidRecords().ToList();
            var hasValue = records != null;
            packer.WriteBit(hasValue);
            if (!hasValue)
                return;

            packer.WriteInteger(records.Count, 31);
            foreach (var record in records)
                Packer<AVisualPresetRecord>.Write(packer, record);
        }

        public static void Read(this BitPacker packer, ref VisualPreset value)
        {
            if (!packer.ReadBit())
            {
                value = null;
                return;
            }

            long count = 0;
            packer.ReadInteger(ref count, 31);

            var records = new List<AVisualPresetRecord>((int) count);
            for (var i = 0; i < count; i++)
            {
                AVisualPresetRecord record = null;
                Packer<AVisualPresetRecord>.Read(packer, ref record);
                if (record != null)
                    records.Add(record);
            }

            value = new VisualPreset(records);
        }
        
        public static void Write(this BitPacker packer, ColorVisualPresetRecord value)
        {
            packer.CustomWriteTranslatedDefinition(value?.VisualID);
            Packer<Color>.Write(packer, value?.Color ?? default);
        }

        public static void Read(this BitPacker packer, ref ColorVisualPresetRecord value)
        {
            var id = packer.CustomReadTranslatedDefinition<ColorVisualID>();
            Color color = default;
            Packer<Color>.Read(packer, ref color);
            value = new ColorVisualPresetRecord(id, color);
        }

        public static void Write(this BitPacker packer, StringVisualPresetRecord value)
        {
            packer.CustomWriteTranslatedDefinition(value?.VisualID);
            packer.WriteString(Encoding.UTF8, value?.Text ?? string.Empty);
        }

        public static void Read(this BitPacker packer, ref StringVisualPresetRecord value)
        {
            var id = packer.CustomReadTranslatedDefinition<StringVisualID>();
            var text = packer.ReadString(Encoding.UTF8);
            value = new StringVisualPresetRecord(id, text);
        }
        
        public static void Write(this BitPacker packer, MaterialVisualPresetRecord value)
        {
            packer.CustomWriteTranslatedDefinition(value?.VisualID);
            Packer.WriteAsNetworkAsset(packer, value?.Material);
        }

        public static void Read(this BitPacker packer, ref MaterialVisualPresetRecord value)
        {
            var id = packer.CustomReadTranslatedDefinition<MaterialVisualID>();
            Material material = null;
            Packer.ReadAsNetworkAsset(packer, ref material);
            value = new MaterialVisualPresetRecord(id, material);
        }

        public static void Write(this BitPacker packer, SpriteVisualPresetRecord value)
        {
            packer.CustomWriteTranslatedDefinition(value?.VisualID);
            Packer.WriteAsNetworkAsset(packer, value?.Sprite);
        }

        public static void Read(this BitPacker packer, ref SpriteVisualPresetRecord value)
        {
            var id = packer.CustomReadTranslatedDefinition<SpriteVisualID>();
            Sprite sprite = null;
            Packer.ReadAsNetworkAsset(packer, ref sprite);
            value = new SpriteVisualPresetRecord(id, sprite);
        }

        public static void Write(this BitPacker packer, PrefabVisualPresetRecord value)
        {
            packer.CustomWriteTranslatedDefinition(value?.VisualID);
            Packer.WriteAsNetworkAsset(packer, value?.Prefab);
        }

        public static void Read(this BitPacker packer, ref PrefabVisualPresetRecord value)
        {
            var id = packer.CustomReadTranslatedDefinition<PrefabVisualID>();
            GameObject prefab = null;
            Packer.ReadAsNetworkAsset(packer, ref prefab);
            value = new PrefabVisualPresetRecord(id, prefab);
        }
    }
}
#endif
