// Author: František Holubec
// Created: 21.10.2026

#if DEBUG_MENU
using System.IO;
using EDIVE.Debugging;
using EDIVE.External.DomainReloadHelper;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EDIVE.Rendering.GraphicsWarmup
{
    // Records graphics states for GraphicsWarmupLoader. Record on the target device.
    public static class GraphicsWarmupDebugMenu
    {
        [ClearOnReload]
        private static GraphicsStateCollection _trace;

        [ClearOnReload]
        private static bool _registered;

        public static void Register()
        {
            if (_registered)
                return;

            _registered = true;
            DebugMenu.Panel("Rendering").Icon("palette");
            var section = DebugMenu.Section("Rendering", "Graphics states");
            section.Button("Start recording", StartRecording);
            section.Button("Stop and save", StopAndSave);
            section.Watch("Recorded", () => _trace != null ? _trace.totalGraphicsStateCount : 0);
        }

        private static void StartRecording()
        {
            if (_trace != null)
            {
                DebugMenu.Toast("Already recording", DebugToastLevel.Warning);
                return;
            }

            _trace = new GraphicsStateCollection();
            if (!_trace.BeginTrace())
            {
                _trace = null;
                DebugMenu.Toast("Recording is not supported", DebugToastLevel.Warning);
                return;
            }

            DebugMenu.Toast("Recording graphics states");
        }

        private static void StopAndSave()
        {
            if (_trace == null)
            {
                DebugMenu.Toast("Not recording", DebugToastLevel.Warning);
                return;
            }

            _trace.EndTrace();

            var quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            var fileName = $"GraphicsStates_{Application.platform}_{SystemInfo.graphicsDeviceType}_{quality}";
            var directory = Path.Combine(Application.persistentDataPath, "GraphicsStates");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"{fileName}.graphicsstate");

            var saved = _trace.SaveToFile(path);
            var sent = _trace.SendToEditor(fileName);
            var count = _trace.totalGraphicsStateCount;
            _trace = null;

            Debug.Log($"[GraphicsWarmup] {count} states. Saved: {(saved ? path : "failed")}. Sent to editor: {sent}.");
            DebugMenu.Toast(sent ? $"{count} states sent to editor" : $"{count} states saved to {path}",
                saved || sent ? DebugToastLevel.Success : DebugToastLevel.Warning);
        }
    }
}
#endif
