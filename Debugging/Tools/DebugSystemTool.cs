// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            app.Watch("App ID", () => Application.identifier);
            app.Watch("Unity", () => Application.unityVersion);
            app.Watch("Unity Pro", () => Application.HasProLicense());
            app.Watch("Genuine", () => !Application.genuineCheckAvailable ? "Unknown" : Application.genuine ? "Yes" : "No");
            app.Watch("Platform", () => Application.platform);
            app.Watch("Build", () => Debug.isDebugBuild ? "Development" : "Release");
            app.Watch("Backend", () => ScriptingBackend);
            app.Watch("Install mode", () => Application.installMode);
            app.Watch("Sandbox", () => Application.sandboxType);
            app.Watch("Language", () => Application.systemLanguage);
            app.Watch("Uptime", () => TimeSpan.FromSeconds(Time.realtimeSinceStartupAsDouble).ToString(@"hh\:mm\:ss"));
            app.Watch("Scene", () => SceneManager.GetActiveScene().name);
            app.Watch("Scene time", () => TimeSpan.FromSeconds(Time.timeSinceLevelLoadAsDouble).ToString(@"hh\:mm\:ss"));

            var device = page.Section("Device");
            device.Watch("Name", () => SystemInfo.deviceName);
            device.Watch("Model", () => SystemInfo.deviceModel);
            device.Watch("Type", () => SystemInfo.deviceType);
            device.Watch("OS", () => SystemInfo.operatingSystem);
            device.Watch("CPU", () => SystemInfo.processorType);
            device.Watch("Cores", () => SystemInfo.processorCount);
            device.Watch("RAM", () => SystemInfo.systemMemorySize).Unit("MB");
            device.Watch("GPU", () => SystemInfo.graphicsDeviceName);
            device.Watch("GPU vendor", () => SystemInfo.graphicsDeviceVendor);
            device.Watch("VRAM", () => SystemInfo.graphicsMemorySize).Unit("MB");
            device.Watch("Graphics API", () => SystemInfo.graphicsDeviceVersion);
            device.Watch("Max texture", () => SystemInfo.maxTextureSize);
#if UNITY_ANDROID && !UNITY_EDITOR
            device.Watch("Thermal", () => ThermalStatus);
#endif
            device.Watch("Internet", () => Application.internetReachability switch
            {
                NetworkReachability.ReachableViaLocalAreaNetwork => "Wi-Fi / LAN",
                NetworkReachability.ReachableViaCarrierDataNetwork => "Mobile data",
                _ => "None"
            });

            if (SystemInfo.batteryStatus != BatteryStatus.Unknown)
            {
                var battery = page.Section("Battery");
                battery.Watch("Status", () => SystemInfo.batteryStatus);
                battery.Watch("Level", () => SystemInfo.batteryLevel * 100f).Format("0").Unit("%");
            }

            var features = page.Section("Features");
            features.Watch("Location", () => SystemInfo.supportsLocationService);
            features.Watch("Accelerometer", () => SystemInfo.supportsAccelerometer);
            features.Watch("Gyroscope", () => SystemInfo.supportsGyroscope);
            features.Watch("Vibration", () => SystemInfo.supportsVibration);
            features.Watch("Audio", () => SystemInfo.supportsAudio);

#if UNITY_IOS
            var ios = page.Section("iOS");
            ios.Watch("Generation", () => UnityEngine.iOS.Device.generation);
            ios.Watch("Ad tracking", () => UnityEngine.iOS.Device.advertisingTrackingEnabled);
#endif

            var display = page.Section("Display");
            display.Watch("Resolution", () => $"{Screen.width} x {Screen.height}");
            display.Watch("Fullscreen", () => Screen.fullScreen);
            display.Watch("Fullscreen mode", () => Screen.fullScreenMode);
            display.Watch("DPI", () => Screen.dpi).Format("0");
            display.Watch("Refresh rate", () => Screen.currentResolution.refreshRateRatio.value).Format("0.#").Unit("Hz");
            display.Watch("Orientation", () => Screen.orientation);
            display.Watch("Quality", () => QualitySettings.names[QualitySettings.GetQualityLevel()]);
            display.Watch("Target FPS", () => Application.targetFrameRate);
            display.Watch("VSync", () => QualitySettings.vSyncCount);

            var xr = page.Section("XR");
            xr.Watch("Active", () => XRSettings.isDeviceActive);
            xr.Watch("Device", () => string.IsNullOrEmpty(XRSettings.loadedDeviceName) ? "-" : XRSettings.loadedDeviceName);
            xr.Watch("Headset", () => OrDash(InputDevices.GetDeviceAtXRNode(XRNode.Head).name));
            xr.Watch("Controllers", () => OrDash(DeviceNames(InputDeviceCharacteristics.Controller)));
            xr.Watch("Tracking origin", () => TrackingOrigin);
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

        private static string OrDash(string value) => string.IsNullOrEmpty(value) ? "-" : value;

        private static string DeviceNames(InputDeviceCharacteristics characteristics)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            return string.Join(", ", devices.Select(d => d.name));
        }

        private static object TrackingOrigin
        {
            get
            {
                var subsystems = new List<XRInputSubsystem>();
                SubsystemManager.GetSubsystems(subsystems);
                return subsystems.Count > 0 ? subsystems[0].GetTrackingOriginMode() : "-";
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static readonly string[] THERMAL_STATUSES = {"None", "Light", "Moderate", "Severe", "Critical", "Emergency", "Shutdown"};
        private static AndroidJavaObject _powerManager;

        // Android PowerManager, API 29+
        private static string ThermalStatus
        {
            get
            {
                try
                {
                    if (_powerManager == null)
                    {
                        using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                        using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                        _powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");
                    }
                    var status = _powerManager.Call<int>("getCurrentThermalStatus");
                    return status >= 0 && status < THERMAL_STATUSES.Length ? THERMAL_STATUSES[status] : status.ToString();
                }
                catch (Exception)
                {
                    return "-";
                }
            }
        }
#endif

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
