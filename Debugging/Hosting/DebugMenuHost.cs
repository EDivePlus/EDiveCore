// Author: František Holubec
// Created: 14.10.2026

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Shows the debug menu in a UIDocument. Screen or world space comes only from the document's PanelSettings.
    [RequireComponent(typeof(UIDocument))]
    public class DebugMenuHost : MonoBehaviour
    {
        private const float CM_PER_INCH = 2.54f;
        private const float FALLBACK_DPI = 96f;

        [Tooltip("Optional, falls back to Resources/DebugMenuSettings.")]
        [SerializeField]
        private DebugMenuSettings _Settings;

        [Tooltip("Corner hold trigger, for screen overlays.")]
        [SerializeField]
        private bool _ShowTrigger = true;

        [SerializeField]
        private bool _ShowCloseButton = true;

        [Tooltip("Pads the menu by the device safe area, for screen overlays.")]
        [SerializeField]
        private bool _ApplySafeArea = true;

        [SerializeField]
        private bool _StartOpen;

        [Tooltip("World space renderer order, above the canvases on the same plane.")]
        [SerializeField]
        private int _WorldSortingOrder = 1000;

        private UIDocument _document;
        private VisualElement _root;
        private DebugMenuView _view;
        private DebugMenuTrigger _trigger;
        private DebugFpsCounter _fps;
        private DebugToastStack _toasts;
        private Collider[] _colliders;
        private bool _worldSortingApplied;
        private BoxCollider _box;
        private Vector3 _boxSize;
        private Vector3 _boxCenter;
        private float _triggerSize;

        public DebugMenuSettings Settings => _Settings ? _Settings : DebugMenuSettings.Default;
        public DebugMenuView View => _view;
        public bool IsOpen => _view?.parent != null;
        public int WorldSortingOrder => _WorldSortingOrder;
        public bool IsWorldSpace => _document != null && _document.panelSettings != null && _document.panelSettings.renderMode == PanelRenderMode.WorldSpace;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError($"{nameof(DebugMenuHost)} on {name} has no UI root, check the UIDocument panel settings.", this);
                return;
            }

            _root.pickingMode = PickingMode.Ignore;
            _root.AddToClassList("dm-host");

            _worldSortingApplied = false;
            ApplyPanelStyles();
            _root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            var settings = Settings;
            _view = new DebugMenuView(_ShowCloseButton);
            _view.CloseRequested += Close;

            if (_ShowTrigger)
            {
                _trigger = new DebugMenuTrigger(settings != null ? settings.TriggerHoldTime : 1f);
                _trigger.Activated += () => Open(null);
                _root.Add(_trigger);
            }

            _fps = new DebugFpsCounter();
            _root.Add(_fps);
            _toasts = new DebugToastStack();
            _root.Add(_toasts);

            _root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            // World space input colliders, only active while open so they don't block what's behind
            _colliders = GetComponents<Collider>();
            _box = GetComponent<BoxCollider>();
            if (_box != null)
            {
                _boxSize = _box.size;
                _boxCenter = _box.center;
            }
            SetCollidersEnabled(false);

            DebugMenu.RegisterHost(this);

            if (_StartOpen)
                Open(null);
        }

        private void OnDisable()
        {
            DebugMenu.UnregisterHost(this);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            _root?.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            _view?.RemoveFromHierarchy();
            _trigger?.RemoveFromHierarchy();
            _fps?.RemoveFromHierarchy();
            _toasts?.RemoveFromHierarchy();
            _view = null;
            _trigger = null;
            _fps = null;
            _toasts = null;
            _root = null;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt) => ApplyPanelStyles();

        private void LateUpdate()
        {
            if (!_worldSortingApplied)
                ApplyWorldSorting();
        }

        // Draw and ray order above the canvases on the same plane. The UIRenderer and the EventSystem come later than OnEnable.
        private void ApplyWorldSorting()
        {
            if (!IsWorldSpace)
            {
                _worldSortingApplied = true;
                return;
            }

            var eventSystem = EventSystem.current;
            if (!TryGetComponent<UIRenderer>(out var uiRenderer) || eventSystem == null)
                return;

            uiRenderer.sortingOrder = _WorldSortingOrder;
            if (!eventSystem.TryGetComponent<DebugWorldDocumentRaycaster>(out _))
                eventSystem.gameObject.AddComponent<DebugWorldDocumentRaycaster>();
            _worldSortingApplied = true;
        }

        // On the panel, not the document root, so popups like dropdown lists get them too.
        // The panel settings are only used by the debug menu.
        private void ApplyPanelStyles()
        {
            var settings = Settings;
            var panelRoot = _root?.panel?.visualTree;
            if (settings == null || panelRoot == null)
                return;

            foreach (var styleSheet in settings.StyleSheets)
            {
                if (styleSheet != null && !panelRoot.styleSheets.Contains(styleSheet))
                    panelRoot.styleSheets.Add(styleSheet);
            }

            if (settings.Font != null)
                panelRoot.style.unityFontDefinition = FontDefinition.FromFont(settings.Font);
        }

        public void Open(string location)
        {
            if (_view == null)
                return;

            if (location != null)
                _view.Navigate(location);

            if (_view.parent == null)
                _root.Add(_view);
            // Toasts stay over the menu
            _toasts.BringToFront();
            _fps.SetMenuOpen(true);

            if (_trigger != null)
            {
                _trigger.style.display = DisplayStyle.None;
                _trigger.MarkSeen();
            }
            SetCollidersEnabled(true);
        }

        public void ShowText(DebugTextDocument document)
        {
            Open(null);
            _view?.Show(document);
        }

        public void Close()
        {
            if (_view == null)
                return;

            _view.RemoveFromHierarchy();
            _fps.SetMenuOpen(false);
            if (_trigger != null)
            {
                _trigger.style.display = DisplayStyle.Flex;
                _trigger.MarkSeen();
            }
            SetCollidersEnabled(false);
        }

        public void ShowToast(string text, DebugToastLevel level) => _toasts?.Show(text, level);

        // Hidden while a world space host is active
        internal void SetSuppressed(bool suppressed)
        {
            if (_root == null)
                return;
            if (suppressed)
                Close();
            _root.style.display = suppressed ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open(null);
        }

        // Closed: only a box over the corner stays, so the trigger gets rays and the rest doesn't block
        private void SetCollidersEnabled(bool value)
        {
            if (_colliders == null)
                return;
            foreach (var col in _colliders)
            {
                col.enabled = value;
            }

            if (_box == null)
                return;

            var corner = !value && _trigger != null && _triggerSize > 0f;
            _box.enabled = value || corner;
            if (corner)
            {
                // Document is centered, panel y goes down
                _box.size = new Vector3(_triggerSize, _triggerSize, _boxSize.z);
                _box.center = new Vector3(_boxCenter.x - _boxSize.x * 0.5f + _triggerSize * 0.5f, _boxCenter.y + _boxSize.y * 0.5f - _triggerSize * 0.5f, _boxCenter.z);
            }
            else
            {
                _box.size = _boxSize;
                _box.center = _boxCenter;
            }
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (_ApplySafeArea)
                ApplySafeArea();
            UpdateTriggerSize();
        }

        // Screens: physical size from the DPI. World space: panel units, DPI means nothing there.
        private void UpdateTriggerSize()
        {
            if (_trigger == null || _root?.panel == null)
                return;

            var settings = Settings;
            if (IsWorldSpace)
            {
                SetTriggerSize(settings != null ? settings.WorldTriggerSize : 64f);
                return;
            }

            var rect = _root.layout;
            if (rect.width <= 0f || Screen.width <= 0)
                return;

            var dpi = Screen.dpi > 0f ? Screen.dpi : FALLBACK_DPI;
            var pixels = (settings != null ? settings.TriggerSize : 0.8f) / CM_PER_INCH * dpi;
            SetTriggerSize(pixels * rect.width / Screen.width);
        }

        private void SetTriggerSize(float size)
        {
            if (Mathf.Approximately(size, _triggerSize))
                return;
            _triggerSize = size;
            _trigger.SetSize(size);
            if (!IsOpen)
                SetCollidersEnabled(false);
        }

        // Safe area in screen pixels converted to panel units
        private void ApplySafeArea()
        {
            if (_view == null || _root.panel == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            var rect = _root.layout;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var safe = Screen.safeArea;
            var scaleX = rect.width / Screen.width;
            var scaleY = rect.height / Screen.height;
            _view.style.paddingLeft = safe.xMin * scaleX;
            _view.style.paddingRight = (Screen.width - safe.xMax) * scaleX;
            _view.style.paddingTop = (Screen.height - safe.yMax) * scaleY;
            _view.style.paddingBottom = safe.yMin * scaleY;
        }
    }
}
