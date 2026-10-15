// Author: František Holubec
// Created: 14.10.2026

using System.Collections.Generic;
using EDIVE.External.DomainReloadHelper;
using EDIVE.Utils.FontSymbols;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Project settings, loaded from Resources/DebugMenuSettings.
    [CreateAssetMenu(menuName = "EDIVE/Debug Menu Settings", fileName = RESOURCES_PATH)]
    public class DebugMenuSettings : ScriptableObject
    {
        public const string RESOURCES_PATH = "DebugMenuSettings";

        [Tooltip("Screen overlay created at startup, with the corner trigger.")]
        [SerializeField]
        private bool _CreateOverlay = true;

        [SerializeField]
        private PanelSettings _OverlayPanelSettings;

        [Tooltip("Added to every host, in order. Base DebugMenu.uss first, project theme after.")]
        [SerializeField]
        private List<StyleSheet> _StyleSheets = new();

        [SerializeField]
        private Font _Font;

        [SerializeField]
        private FontSymbolsDefinition _Icons;

        [SerializeField]
        private float _TriggerHoldTime = 1f;

        [Tooltip("Corner size on screens in centimeters, same on every device.")]
        [SerializeField]
        private float _TriggerSize = 0.8f;

        [Tooltip("Corner size on world space panels in panel units.")]
        [SerializeField]
        private float _WorldTriggerSize = 64f;

        [SerializeField]
        private Key _ToggleKey = Key.F1;

        [SerializeField]
        private int _LogCapacity = DebugLogBuffer.DEFAULT_CAPACITY;

        [ClearOnReload]
        private static DebugMenuSettings _default;

        [ClearOnReload]
        private static bool _loaded;

        public bool CreateOverlay => _CreateOverlay;
        public PanelSettings OverlayPanelSettings => _OverlayPanelSettings;
        public IReadOnlyList<StyleSheet> StyleSheets => _StyleSheets;
        public Font Font => _Font;
        public FontSymbolsDefinition Icons => _Icons;
        public float TriggerHoldTime => _TriggerHoldTime;
        public float TriggerSize => _TriggerSize;
        public float WorldTriggerSize => _WorldTriggerSize;
        public Key ToggleKey => _ToggleKey;
        public int LogCapacity => _LogCapacity;

        public static DebugMenuSettings Default
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    _default = Resources.Load<DebugMenuSettings>(RESOURCES_PATH);
                }
                return _default;
            }
        }
    }
}
