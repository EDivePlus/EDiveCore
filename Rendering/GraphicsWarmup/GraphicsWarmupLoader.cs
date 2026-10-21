// Author: František Holubec
// Created: 21.10.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EDIVE.Rendering.GraphicsWarmup
{
    // Compiles shaders and graphics states during loading instead of on first use.
    [Serializable]
    public class GraphicsWarmupLoader : ILoadable
    {
        [SerializeField]
        private List<ShaderVariantCollection> _ShaderVariants = new();

        [SerializeField]
        [Tooltip("Recorded from the debug menu. Only the one matching the device is used.")]
        private List<GraphicsStateCollection> _GraphicsStates = new();

        [SerializeField]
        [Tooltip("Items warmed up per frame.")]
        private int _BatchSize = 32;

        public async UniTask Load(Action<float> progressCallback)
        {
#if DEBUG_MENU
            GraphicsWarmupDebugMenu.Register();
#endif

            // The editor compiles shaders on its own.
            if (Application.isEditor)
                return;

            var states = FindMatchingStates();
            var total = 0;
            foreach (var collection in _ShaderVariants)
            {
                if (collection != null)
                    total += collection.variantCount;
            }
            if (states != null)
                total += states.totalGraphicsStateCount;

            if (total == 0)
                return;

            var done = 0;
            var batchSize = Mathf.Max(1, _BatchSize);
            foreach (var collection in _ShaderVariants)
            {
                if (collection == null)
                    continue;

                while (!collection.WarmUpProgressively(batchSize))
                {
                    progressCallback?.Invoke((done + collection.warmedUpVariantCount) / (float) total);
                    await UniTask.Yield();
                }

                done += collection.variantCount;
            }

            if (states == null)
                return;

            while (states.completedWarmupCount < states.totalGraphicsStateCount)
            {
                var before = states.completedWarmupCount;
                states.WarmUpProgressively(batchSize).Complete();
                if (states.completedWarmupCount == before)
                    break;

                progressCallback?.Invoke((done + states.completedWarmupCount) / (float) total);
                await UniTask.Yield();
            }
        }

        // Prefers the current quality level, falls back to any with the same device.
        private GraphicsStateCollection FindMatchingStates()
        {
            var quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            GraphicsStateCollection fallback = null;
            foreach (var states in _GraphicsStates)
            {
                if (states == null || states.graphicsDeviceType != SystemInfo.graphicsDeviceType || states.runtimePlatform != Application.platform)
                    continue;

                if (states.qualityLevelName == quality)
                    return states;

                fallback ??= states;
            }

            return fallback;
        }
    }
}
