// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

namespace EDIVE.Debugging
{
    // Application, device and display info, read when shown. Live numbers are in the Profiler.
    public sealed class DebugSystemTool : ADebugPageTool
    {
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
            app.Watch("Uptime", () => TimeSpan.FromSeconds(Time.realtimeSinceStartupAsDouble).ToString(@"hh\:mm\:ss"));

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
        }

        protected override VisualElement CreateHeaderContent(DebugPage page) =>
            DebugMenuView.CreateIconButton("content_copy", () => DebugShare.Copy(CreateReport(page)));

        // Every value as text, one section per block
        private static string CreateReport(DebugPage page)
        {
            var report = new StringBuilder();
            foreach (var section in page.VisibleSections)
            {
                if (report.Length > 0)
                    report.AppendLine();
                report.AppendLine(section.Name);
                foreach (var control in section.SortedControls)
                {
                    if (control is DebugWatch watch)
                        report.AppendLine($"{watch.Label}: {watch.GetText()}");
                }
            }
            return report.ToString();
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
