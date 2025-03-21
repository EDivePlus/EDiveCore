using System;
using System.Diagnostics.CodeAnalysis;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.Utils
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum PlayerCompressionType
    {
        [Tooltip("No compression")]Default,
        [LabelText("LZ4")][Tooltip("Moderate compression, supports runtime recompression")]LZ4,
        [LabelText("LZ4HC")][Tooltip("High compression, no runtime recompression.")]LZ4HC
    }

    public static class PlayerCompressionExtensions
    {
        public static BuildOptions ToBuildOptions(this PlayerCompressionType compression) => compression switch
        {
            PlayerCompressionType.Default => BuildOptions.None,
            PlayerCompressionType.LZ4 => BuildOptions.CompressWithLz4,
            PlayerCompressionType.LZ4HC => BuildOptions.CompressWithLz4HC,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
