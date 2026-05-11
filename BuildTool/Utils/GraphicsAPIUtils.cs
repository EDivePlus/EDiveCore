// Author: František Holubec
// Created: 18.03.2026

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Rendering;

namespace EDIVE.BuildTool.Utils
{
    public static class GraphicsAPIUtils
    {
        private static readonly Dictionary<BuildTarget, GraphicsDeviceType[]> CACHE = new();
        private static readonly MethodInfo GET_SUPPORTED_METHOD = typeof(PlayerSettings).GetMethod(
            "GetSupportedGraphicsAPIs",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null, new[] { typeof(BuildTarget) }, null);

        public static GraphicsDeviceType[] GetSupportedGraphicsAPIs(BuildTarget target)
        {
            if (CACHE.TryGetValue(target, out var cached))
                return cached;

            var result = GET_SUPPORTED_METHOD?.Invoke(null, new object[] { target }) as GraphicsDeviceType[] ?? Array.Empty<GraphicsDeviceType>();
            CACHE[target] = result;
            return result;
        }
    }
}
