// Author: František Holubec
// Created: 07.07.2025

#if MEMORY_PACK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EDIVE.NativeUtils;
using MemoryPack;
using MemoryPack.Formatters;
using UnityEngine;

namespace EDIVE.Utils.MemoryPack
{
    public static class MemoryPackUtility
    {
        public static void RegisterDynamicUnionFormatter<T>() where T : class
        {
            var taggedTypes = new List<(ushort Tag, Type Type)>();
            foreach (var type in typeof(T).GetAssignableTypes())
            {
                var tagAttr = type.GetCustomAttribute<MemoryPackUnionTagAttribute>();
                if (tagAttr == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[MemoryPack] Skipping {type.FullName} — missing [MemoryPackUnionTag] attribute.");
#endif
                    continue;
                }
                taggedTypes.Add((tagAttr.Tag, type));
            }

#if UNITY_EDITOR
            var duplicateTags = taggedTypes
                .GroupBy(t => t.Tag)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateTags.Any())
            {
                foreach (var group in duplicateTags)
                {
                    Debug.LogError($"[MemoryPack] Duplicate tag {group.Key} used by: {string.Join(", ", group.Select(t => t.Type.Name))}");
                }
            }
#endif

            var formatter = new DynamicUnionFormatter<T>(taggedTypes.ToArray());
            MemoryPackFormatterProvider.Register(formatter);
        }
    }
}
#endif
