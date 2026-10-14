// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EDIVE.View.EditorPreview
{
    // Builds the active preset's placeholders around UI prefabs in Prefab Mode. Nothing is saved
    [InitializeOnLoad]
    public static class PreviewEnvironment
    {
        public const string OFF = "Off";
        public const string AUTO = "Auto";

        private const string NAME_PREFIX = "[Preview] ";
        private const HideFlags PREVIEW_FLAGS = HideFlags.DontSave | HideFlags.NotEditable;

        private static GlobalPersistentContext<string> _selectionContext;
        private static GlobalPersistentContext<string> _areaContext;
        private static List<PreviewEnvironmentPreset> _presets;

        private static readonly List<GameObject> CREATED = new();
        private static PrefabStage _stage;
        private static Canvas _canvas;
        private static Transform _root;
        private static ScalerState? _scalerBackup;
        private static string _signature;
        private static bool _dirty = true;
        private static MethodInfo _setExpanded;

        public static event Action Changed;

        public static string PresetSelection
        {
            get => SelectionContext.Value;
            set
            {
                SelectionContext.Value = value;
                SetDirty();
            }
        }

        // Name of the preset area the prefab is laid out in
        public static string AreaSelection
        {
            get => AreaContext.Value;
            set
            {
                AreaContext.Value = value;
                SetDirty();
            }
        }

        public static PreviewEnvironmentPreset ActivePreset { get; private set; }
        public static PreviewContentArea ActiveArea { get; private set; }

        public static bool IsAvailable => FindEnvironmentCanvas(PrefabStageUtility.GetCurrentPrefabStage()) != null;

        public static IReadOnlyList<PreviewEnvironmentPreset> Presets => _presets ??= EditorAssetUtils.FindAllAssetsOfType<PreviewEnvironmentPreset>().OrderBy(p => p.name).ToList();

        private static GlobalPersistentContext<string> SelectionContext => _selectionContext ??= PersistentContext.Get("EDIVE.PreviewEnvironment", "Preset", AUTO);
        private static GlobalPersistentContext<string> AreaContext => _areaContext ??= PersistentContext.Get("EDIVE.PreviewEnvironment", "Area", string.Empty);

        static PreviewEnvironment()
        {
            PrefabStage.prefabStageOpened += _ => SetDirty();
            PrefabStage.prefabStageClosing += _ => Clear();
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.update += OnUpdate;
        }

        public static void SetDirty() => _dirty = true;

        private static void OnProjectChanged()
        {
            _presets = null;
            SetDirty();
        }

        private static void OnUpdate()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
            {
                if (_stage != null)
                    Clear();
                return;
            }

            if (!_dirty && stage == _stage && ComputeSignature() == _signature)
                return;

            _dirty = false;
            Rebuild(stage);
        }

        private static void Rebuild(PrefabStage stage)
        {
            var selection = Selection.objects;
            Clear();
            _stage = stage;
            _canvas = FindEnvironmentCanvas(stage);
            _root = stage.prefabContentsRoot.transform;
            ActivePreset = _canvas != null ? ResolvePreset() : null;
            _signature = ComputeSignature();

            if (ActivePreset != null)
                Build(ActivePreset);

            Selection.objects = selection;
            EditorApplication.delayCall += RevealRoot;
            Changed?.Invoke();
        }

        // Preview parents are new each rebuild, so the hierarchy shows them collapsed
        private static void RevealRoot()
        {
            if (_root == null)
                return;

            _setExpanded ??= typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow")?
                .GetMethod("SetExpanded", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(int), typeof(bool) }, null);
            if (_setExpanded == null)
                return;

            var windows = Resources.FindObjectsOfTypeAll(_setExpanded.DeclaringType);
            for (var current = _root; current != null; current = current.parent)
            {
                foreach (var window in windows)
                    _setExpanded.Invoke(window, new object[] { current.gameObject.GetInstanceID(), true });
            }
        }

        private static void Build(PreviewEnvironmentPreset preset)
        {
            RemoveLeftovers();
            if (_root.parent != _canvas.transform)
                return;

            if (preset.OverrideScaler && _canvas.TryGetComponent<CanvasScaler>(out var scaler))
            {
                _scalerBackup = ScalerState.From(scaler);
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.referenceResolution = preset.ReferenceResolution;
                scaler.matchWidthOrHeight = preset.MatchWidthOrHeight;
            }

            var screen = (RectTransform) _canvas.transform;
            var safeArea = CreateRect("Safe Area", screen);
            ApplySafeArea(safeArea);

            var placeholders = preset.Placeholders.Where(p => p != null).ToList();
            foreach (var placeholder in placeholders)
            {
                var rect = CreateRect(placeholder.Title, placeholder.InSafeArea ? safeArea : screen);
                placeholder.Layout(rect);
                placeholder.Build(rect);
            }

            ActiveArea = preset.GetArea(AreaSelection);
            var content = CreateRect("Content", ActiveArea.InSafeArea ? safeArea : screen);
            var reserved = ActiveArea.AvoidPlaceholders ? placeholders.Where(p => p.ReservesSpace && p.InSafeArea == ActiveArea.InSafeArea) : Enumerable.Empty<APreviewPlaceholder>();
            ApplyInsets(content, reserved, ActiveArea.Padding);

            _root.SetParent(content, false);

            // Edited prefab listed and drawn last
            for (Transform current = content; current != _canvas.transform; current = current.parent)
                current.SetAsLastSibling();

            foreach (var child in CREATED.SelectMany(c => c.GetComponentsInChildren<Transform>(true)).Where(c => !c.IsChildOf(_root)))
                child.gameObject.hideFlags = PREVIEW_FLAGS;
        }

        private static void Clear()
        {
            if (_root != null && _canvas != null && _root.parent != _canvas.transform)
                _root.SetParent(_canvas.transform, false);

            foreach (var created in CREATED.Where(c => c != null))
                Object.DestroyImmediate(created);
            CREATED.Clear();

            if (_scalerBackup.HasValue && _canvas != null && _canvas.TryGetComponent<CanvasScaler>(out var scaler))
                _scalerBackup.Value.ApplyTo(scaler);
            _scalerBackup = null;

            _stage = null;
            _canvas = null;
            _root = null;
            ActivePreset = null;
            ActiveArea = null;
        }

        // Objects left behind when the editor skipped Clear
        private static void RemoveLeftovers()
        {
            if (_root.parent != _canvas.transform && _root.parent != null && _root.parent.name.StartsWith(NAME_PREFIX))
                _root.SetParent(_canvas.transform, false);

            foreach (var child in _canvas.transform.Cast<Transform>().Where(c => c != _root && c.name.StartsWith(NAME_PREFIX)).ToList())
                Object.DestroyImmediate(child.gameObject);
        }

        private static PreviewEnvironmentPreset ResolvePreset()
        {
            var selection = PresetSelection;
            if (selection == OFF)
                return null;

            if (selection != AUTO)
            {
                var selected = Presets.FirstOrDefault(p => p != null && p.name == selection);
                if (selected != null)
                    return selected;
            }

            var model = UnityEngine.Device.SystemInfo.deviceModel;
            var type = UnityEngine.Device.SystemInfo.deviceType;
            return Presets
                .Where(p => p != null)
                .Select(p => (Preset: p, Score: p.GetMatchScore(model, type)))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Preset)
                .FirstOrDefault();
        }

        private static Canvas FindEnvironmentCanvas(PrefabStage stage)
        {
            if (stage == null || stage.prefabContentsRoot == null)
                return null;

            var top = stage.prefabContentsRoot.transform.root;
            return top != stage.prefabContentsRoot.transform && top.TryGetComponent<Canvas>(out var canvas) ? canvas : null;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(NAME_PREFIX + name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _stage.scene);
            var rect = (RectTransform) go.transform;
            rect.SetParent(parent, false);
            rect.SetToFillParent();
            if (parent == _canvas.transform)
                CREATED.Add(go);
            return rect;
        }

        private static void ApplySafeArea(RectTransform rect)
        {
            var safeArea = GetSafeAreaAnchors();
            rect.anchorMin = safeArea.min;
            rect.anchorMax = safeArea.max;
        }

        // Safe area as anchors, the simulator provides the device values
        private static Rect GetSafeAreaAnchors()
        {
            var width = (float) UnityEngine.Device.Screen.width;
            var height = (float) UnityEngine.Device.Screen.height;
            if (width <= 0 || height <= 0)
                return new Rect(0, 0, 1, 1);

            var safeArea = UnityEngine.Device.Screen.safeArea;
            var min = new Vector2(Mathf.Clamp01(safeArea.xMin / width), Mathf.Clamp01(safeArea.yMin / height));
            var max = new Vector2(Mathf.Clamp01(safeArea.xMax / width), Mathf.Clamp01(safeArea.yMax / height));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void ApplyInsets(RectTransform rect, IEnumerable<APreviewPlaceholder> placeholders, Vector4 padding)
        {
            float left = 0, bottom = 0, right = 0, top = 0;
            foreach (var placeholder in placeholders)
            {
                switch (placeholder.Edge)
                {
                    case PreviewEdge.Bottom: bottom = Mathf.Max(bottom, placeholder.ReservedSize); break;
                    case PreviewEdge.Top: top = Mathf.Max(top, placeholder.ReservedSize); break;
                    case PreviewEdge.Left: left = Mathf.Max(left, placeholder.ReservedSize); break;
                    case PreviewEdge.Right: right = Mathf.Max(right, placeholder.ReservedSize); break;
                    case PreviewEdge.Center:
                    case PreviewEdge.Fill:
                    default: break;
                }
            }
            rect.offsetMin = new Vector2(left + padding.x, bottom + padding.y);
            rect.offsetMax = new Vector2(-(right + padding.z), -(top + padding.w));
        }

        private static string ComputeSignature()
        {
            if (_canvas == null)
                return string.Empty;

            // No sizes, they differ per editor view and would rebuild every repaint
            return string.Join("|",
                UnityEngine.Device.SystemInfo.deviceModel,
                UnityEngine.Device.Screen.orientation,
                PresetSelection,
                AreaSelection);
        }

        private readonly struct ScalerState
        {
            private readonly CanvasScaler.ScaleMode _mode;
            private readonly CanvasScaler.ScreenMatchMode _matchMode;
            private readonly Vector2 _referenceResolution;
            private readonly float _match;

            private ScalerState(CanvasScaler scaler)
            {
                _mode = scaler.uiScaleMode;
                _matchMode = scaler.screenMatchMode;
                _referenceResolution = scaler.referenceResolution;
                _match = scaler.matchWidthOrHeight;
            }

            public static ScalerState From(CanvasScaler scaler) => new(scaler);

            public void ApplyTo(CanvasScaler scaler)
            {
                scaler.uiScaleMode = _mode;
                scaler.screenMatchMode = _matchMode;
                scaler.referenceResolution = _referenceResolution;
                scaler.matchWidthOrHeight = _match;
            }
        }
    }
}
