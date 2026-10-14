// Author: František Holubec
// Created: 14.10.2026

using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Shows the debug menu in a UIDocument. Screen or world space comes only from the document's PanelSettings.
    [RequireComponent(typeof(UIDocument))]
    public class DebugMenuHost : MonoBehaviour
    {
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

        private UIDocument _document;
        private VisualElement _root;
        private DebugMenuView _view;
        private DebugMenuTrigger _trigger;

        public DebugMenuSettings Settings => _Settings ? _Settings : DebugMenuSettings.Default;
        public DebugMenuView View => _view;
        public bool IsOpen => _view?.parent != null;

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

            var settings = Settings;
            if (settings != null)
            {
                foreach (var styleSheet in settings.StyleSheets)
                {
                    if (styleSheet != null && !_root.styleSheets.Contains(styleSheet))
                        _root.styleSheets.Add(styleSheet);
                }

                if (settings.Font != null)
                    _root.style.unityFontDefinition = FontDefinition.FromFont(settings.Font);
            }

            _view = new DebugMenuView(_ShowCloseButton);
            _view.CloseRequested += Close;

            if (_ShowTrigger)
            {
                _trigger = new DebugMenuTrigger(settings != null ? settings.TriggerHoldTime : 1f);
                _trigger.Activated += () => Open(null);
                _root.Add(_trigger);
            }

            if (_ApplySafeArea)
                _root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            DebugMenu.RegisterHost(this);

            if (_StartOpen)
                Open(null);
        }

        private void OnDisable()
        {
            DebugMenu.UnregisterHost(this);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            _view?.RemoveFromHierarchy();
            _trigger?.RemoveFromHierarchy();
            _view = null;
            _trigger = null;
            _root = null;
        }

        public void Open(string location)
        {
            if (_view == null)
                return;

            if (location != null)
                _view.Navigate(location);

            if (_view.parent == null)
                _root.Add(_view);

            if (_trigger != null)
                _trigger.style.display = DisplayStyle.None;
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
            if (_trigger != null)
                _trigger.style.display = DisplayStyle.Flex;
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open(null);
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt) => ApplySafeArea();

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
