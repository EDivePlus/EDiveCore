// Author: František Holubec
// Created: 03.06.2026

using System.Collections.Generic;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Utils.SystemInformation
{
    public class SystemInfoManager : MonoBehaviour
    {
        private List<SystemInfoCategory> _categories;
        public IReadOnlyList<SystemInfoCategory> Categories => _categories ?? PrepareCategories();
        
        private SystemInfoCategory GetOrCreateCategory(string categoryName)
        {
            if (_categories.TryGetFirst(c => c.Name == categoryName, out var existing))
                return existing;

            var newCategory = new SystemInfoCategory(categoryName);
            _categories.Add(newCategory);
            return newCategory;
        }
        
        private List<SystemInfoCategory> PrepareCategories()
        {
            _categories = new List<SystemInfoCategory>();
            
#if ENABLE_IL2CPP
            const string il2CPP = "Yes";
#else
            const string il2CPP = "No";
#endif
            
            GetOrCreateCategory("Unity").AddRange(new List<SystemInfoEntry>
            {
                new("Unity Version", Application.unityVersion),
                new("Debug", Debug.isDebugBuild.ToString()),
                new("Unity Pro", Application.HasProLicense().ToString()),
                new("Genuine", $"{(Application.genuine ? "Yes" : "No")} ({(Application.genuineCheckAvailable ? "Trusted" : "Untrusted")})"),
                new("System Language", Application.systemLanguage.ToString()),
                new("Platform", Application.platform.ToString()),
                new("Install Mode", Application.installMode.ToString()),
                new("Sandbox", Application.sandboxType.ToString()),
                new("IL2CPP", il2CPP),
                new("App Version", Application.version),
                new("App Id", Application.identifier),
            });
            
            GetOrCreateCategory("System").AddRange(new List<SystemInfoEntry>
            {
                new("Operating System", SystemInfo.operatingSystem),
                new("Device Name", SystemInfo.deviceName),
                new("Device Type", SystemInfo.deviceType.ToString()),
                new("Device Model", SystemInfo.deviceModel),            
                new("CPU Type", SystemInfo.processorType),
                new("CPU Count", SystemInfo.processorCount.ToString()),
                new("System Memory", $"{GetBytesReadable((long) SystemInfo.systemMemorySize*1024*1024)}")
            });

            GetOrCreateCategory("Display").AddRange(new List<SystemInfoEntry>
            {
                new("Resolution", () => $"{Screen.width}x{Screen.height}"),
                new("DPI", () => $"{Screen.dpi}"),
                new("Fullscreen", () => $"{Screen.fullScreen}"),
                new("Fullscreen Mode", () => $"{Screen.fullScreenMode}"),
                new("Orientation", () => $"{Screen.orientation}"),
            });
            
            if (SystemInfo.batteryStatus != BatteryStatus.Unknown)
            {
                GetOrCreateCategory("Battery").AddRange(new List<SystemInfoEntry>
                {
                    new("Status", () => $"{SystemInfo.batteryStatus}"),
                    new("Battery Level", () => $"{SystemInfo.batteryLevel}")
                });
            }

            GetOrCreateCategory("Runtime").AddRange(new List<SystemInfoEntry>
            {
                new("Play Time", () => $"{Time.unscaledTime}"),
                new("Level Play Time", () => $"{Time.timeSinceLevelLoad}"),
                new("Current Level", () =>
                {
                    var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    return $"{activeScene.name} (Index: {activeScene.buildIndex})";
                }),
                new("Quality Level", () => $"{QualitySettings.names[QualitySettings.GetQualityLevel()]} ({QualitySettings.GetQualityLevel()})"),
            });
            
            GetOrCreateCategory("Features").AddRange(new List<SystemInfoEntry>
            {
                new("Location", SystemInfo.supportsLocationService.ToString()),
                new("Accelerometer", SystemInfo.supportsAccelerometer.ToString()),
                new("Gyroscope", SystemInfo.supportsGyroscope.ToString()),
                new("Vibration", SystemInfo.supportsVibration.ToString()),
                new("Audio", SystemInfo.supportsAudio.ToString()),
            });

#if UNITY_IOS
            GetOrCreateCategory("iOS").AddRange(new List<SystemInfoEntry>
            {
                new("Generation", UnityEngine.iOS.Device.generation.ToString()),
                new("Ad Tracking", UnityEngine.iOS.Device.advertisingTrackingEnabled.ToString()),
            });
#endif

            GetOrCreateCategory("Graphics - Device").AddRange(new List<SystemInfoEntry>
            {
                new("Device Name", SystemInfo.graphicsDeviceName),
                new("Device Vendor", SystemInfo.graphicsDeviceVendor),
                new("Device Version", SystemInfo.graphicsDeviceVersion),
                new("Graphics Memory", GetBytesReadable((long) SystemInfo.graphicsMemorySize * 1024 * 1024)),
                new("Max Texture Size", SystemInfo.maxTextureSize.ToString()),
            });
            
            return _categories;
        }

        private static string GetBytesReadable(long value)
        {
            var sign = value < 0 ? "-" : "";
            double readable;
            string suffix;
            switch (value)
            {
                case >= 0x1000000000000000:
                    suffix = "EB";
                    readable = value >> 50;
                    break;
                case >= 0x4000000000000:
                    suffix = "PB";
                    readable = value >> 40;
                    break;
                case >= 0x10000000000:
                    suffix = "TB";
                    readable = value >> 30;
                    break;
                case >= 0x40000000:
                    suffix = "GB";
                    readable = value >> 20;
                    break;
                case >= 0x100000:
                    suffix = "MB";
                    readable = value >> 10;
                    break;
                case >= 0x400:
                    suffix = "KB";
                    readable = value;
                    break;
                default: return value.ToString(sign + "0 B");
            }
            readable /= 1024;
            return sign + readable.ToString("0.### ") + suffix;
        }
    }
}
