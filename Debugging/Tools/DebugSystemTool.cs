// Author: František Holubec
// Created: 14.10.2026

using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR;

namespace EDIVE.Debugging
{
    // Application, device and runtime info, read when shown.
    public sealed class DebugSystemTool : ADebugPageTool
    {
        private const float MB = 1024f * 1024f;

        public override string Name => "System";
        public override string Icon => "info";
        public override int Order => -90;

        protected override void Build(DebugPage page)
        {
            var app = page.Section("Application");
            app.Watch("Product", () => Application.productName);
            app.Watch("Version", () => Application.version);
            app.Watch("Unity", () => Application.unityVersion);
            app.Watch("Platform", () => Application.platform);
            app.Watch("Build", () => Debug.isDebugBuild ? "Development" : "Release");
            app.Watch("Backend", () => ScriptingBackend);
            app.Watch("Language", () => Application.systemLanguage);

            var device = page.Section("Device");
            device.Watch("Model", () => SystemInfo.deviceModel);
            device.Watch("Type", () => SystemInfo.deviceType);
            device.Watch("OS", () => SystemInfo.operatingSystem);
            device.Watch("CPU", () => SystemInfo.processorType);
            device.Watch("Cores", () => SystemInfo.processorCount);
            device.Watch("RAM", () => SystemInfo.systemMemorySize).Unit("MB");
            device.Watch("GPU", () => SystemInfo.graphicsDeviceName);
            device.Watch("VRAM", () => SystemInfo.graphicsMemorySize).Unit("MB");
            device.Watch("Graphics API", () => SystemInfo.graphicsDeviceVersion);

            var display = page.Section("Display");
            display.Watch("Resolution", () => $"{Screen.width} x {Screen.height}");
            display.Watch("DPI", () => Screen.dpi).Format("0");
            display.Watch("Refresh rate", () => Screen.currentResolution.refreshRateRatio.value).Format("0.#").Unit("Hz");
            display.Watch("Orientation", () => Screen.orientation);
            display.Watch("Quality", () => QualitySettings.names[QualitySettings.GetQualityLevel()]);
            display.Watch("Target FPS", () => Application.targetFrameRate);
            display.Watch("VSync", () => QualitySettings.vSyncCount);

            var xr = page.Section("XR");
            xr.Watch("Active", () => XRSettings.isDeviceActive);
            xr.Watch("Device", () => string.IsNullOrEmpty(XRSettings.loadedDeviceName) ? "-" : XRSettings.loadedDeviceName);
            xr.Watch("Eye texture", () => $"{XRSettings.eyeTextureWidth} x {XRSettings.eyeTextureHeight}");
            xr.Watch("Render scale", () => XRSettings.eyeTextureResolutionScale).Format("0.##");

            var runtime = page.Section("Runtime");
            runtime.Watch("Uptime", () => TimeSpan.FromSeconds(Time.realtimeSinceStartupAsDouble).ToString(@"hh\:mm\:ss")).Every(1f);
            runtime.Watch("Frame", () => Time.frameCount).Every(0.25f);
            runtime.Watch("FPS", () => 1f / Mathf.Max(0.0001f, Time.smoothDeltaTime)).Format("0").Every(0.25f);
            runtime.Watch("Allocated", () => Profiler.GetTotalAllocatedMemoryLong() / MB).Format("0.0").Unit("MB").Every(1f);
            runtime.Watch("Reserved", () => Profiler.GetTotalReservedMemoryLong() / MB).Format("0.0").Unit("MB").Every(1f);
            runtime.Watch("Managed", () => GC.GetTotalMemory(false) / MB).Format("0.0").Unit("MB").Every(1f);
            runtime.Buttons(("Collect GC", () => GC.Collect()), ("Unload unused", () => Resources.UnloadUnusedAssets()));
        }

        private static string ScriptingBackend
        {
            get
            {
#if ENABLE_IL2CPP
                return "IL2CPP";
#else
                return "Mono";
#endif
            }
        }
    }
}
