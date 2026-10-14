// Author: František Holubec
// Created: 14.10.2026

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Created at startup: built-in tools, log capture, owner cleanup, toggle key and the screen overlay.
    [AddComponentMenu("")]
    internal sealed class DebugMenuRunner : MonoBehaviour
    {
        private const float OWNER_CHECK_INTERVAL = 1f;

        private float _nextOwnerCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeLogs()
        {
            var settings = DebugMenuSettings.Default;
            DebugLogBuffer.Initialize(settings != null ? settings.LogCapacity : DebugLogBuffer.DEFAULT_CAPACITY);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            DebugMenu.AddTool(new DebugConsoleTool());
            DebugMenu.AddTool(new DebugSystemTool());

            var runner = new GameObject("[DebugMenu]");
            DontDestroyOnLoad(runner);
            runner.AddComponent<DebugMenuRunner>();

            var settings = DebugMenuSettings.Default;
            if (settings == null)
            {
                Debug.LogWarning($"DebugMenu: no {nameof(DebugMenuSettings)} in Resources, the screen overlay is off.");
                return;
            }

            if (!settings.CreateOverlay || settings.OverlayPanelSettings == null)
                return;

            // Inactive until the document has its panel settings
            var overlay = new GameObject("[DebugMenu Overlay]");
            overlay.SetActive(false);
            DontDestroyOnLoad(overlay);
            var document = overlay.AddComponent<UIDocument>();
            document.panelSettings = settings.OverlayPanelSettings;
            overlay.AddComponent<DebugMenuHost>();
            overlay.SetActive(true);
        }

        private void Update()
        {
            DebugLogBuffer.Flush();

            if (Time.unscaledTime >= _nextOwnerCheck)
            {
                _nextOwnerCheck = Time.unscaledTime + OWNER_CHECK_INTERVAL;
                DebugMenu.CleanupDestroyedOwners();
            }

            var key = DebugMenuSettings.Default != null ? DebugMenuSettings.Default.ToggleKey : Key.F1;
            var keyboard = Keyboard.current;
            if (key != Key.None && keyboard != null && keyboard[key].wasPressedThisFrame)
                DebugMenu.Toggle();
        }
    }
}
