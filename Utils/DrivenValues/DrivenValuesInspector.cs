// Author: František Holubec
// Created: 06.10.2026

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Editor.Drawers;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace EDIVE.Utils.DrivenValues
{
    [InitializeOnLoad]
    internal static class DrivenValuesInspector
    {
        private const double UPDATE_INTERVAL = 0.25;

        private static readonly Type PROPERTY_EDITOR_TYPE = typeof(Editor).Assembly.GetType("UnityEditor.PropertyEditor");
        private static readonly FieldInfo EDITOR_FIELD = typeof(InspectorElement).GetField("m_Editor", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Dictionary<Object, List<DrivenValueInfo>> ENTRIES = new();

        private static bool _dirty = true;
        private static double _nextUpdate;

        static DrivenValuesInspector()
        {
            if (PROPERTY_EDITOR_TYPE == null || EDITOR_FIELD == null)
            {
                Debug.LogWarning("[DrivenValues] Inspector internals changed, driven value boxes are disabled.");
                return;
            }

            ObjectChangeEvents.changesPublished += (ref ObjectChangeEventStream _) => _dirty = true;
            EditorApplication.hierarchyChanged += () => _dirty = true;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (EditorApplication.timeSinceStartup < _nextUpdate)
                return;
            _nextUpdate = EditorApplication.timeSinceStartup + UPDATE_INTERVAL;

            if (_dirty)
                Rebuild();

            foreach (var window in Resources.FindObjectsOfTypeAll(PROPERTY_EDITOR_TYPE).OfType<EditorWindow>())
                window.rootVisualElement.Query<InspectorElement>().ForEach(Refresh);
        }

        private static void Rebuild()
        {
            _dirty = false;
            ENTRIES.Clear();

            var collected = new Dictionary<Object, List<(Component Driver, HashSet<string> Members)>>();
            foreach (var driver in FindDrivers())
            {
                var values = new DrivenValuesCollection();
                try
                {
                    ((IValueDriver) driver).PopulateDrivenValues(values);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, driver);
                    continue;
                }

                foreach (var (target, members) in values.Targets)
                {
                    if (!collected.TryGetValue(target, out var list))
                        collected[target] = list = new List<(Component, HashSet<string>)>();
                    list.Add((driver, members));
                }
            }

            foreach (var (target, drivers) in collected)
                ENTRIES[target] = drivers.Select(d => new DrivenValueInfo(d.Driver, d.Members, drivers)).ToList();
        }

        private static IEnumerable<Component> FindDrivers()
        {
            var drivers = TypeCache.GetTypesDerivedFrom<IValueDriver>()
                .Where(t => !t.IsAbstract && typeof(Component).IsAssignableFrom(t))
                .SelectMany(t => Object.FindObjectsByType(t, FindObjectsInactive.Include, FindObjectsSortMode.None))
                .Cast<Component>();

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                drivers = drivers.Concat(prefabStage.prefabContentsRoot.GetComponentsInChildren<IValueDriver>(true).Cast<Component>());

            return drivers.Distinct();
        }

        private static void Refresh(InspectorElement inspector)
        {
            var editorElement = inspector.parent;
            if (editorElement == null || editorElement.GetType().Name != "EditorElement")
                return;

            if (editorElement.Children().FirstOrDefault(c => c is DrivenValuesView) is not DrivenValuesView view)
            {
                view = new DrivenValuesView();
                editorElement.Insert(editorElement.IndexOf(inspector), view);
            }

            var editor = EDITOR_FIELD.GetValue(inspector) as Editor;
            var infos = editor == null
                ? new List<DrivenValueInfo>()
                : editor.targets
                    .Where(t => t != null && ENTRIES.ContainsKey(t))
                    .SelectMany(t => ENTRIES[t])
                    .GroupBy(i => (i.Driver, i.Message))
                    .Select(g => g.First())
                    .ToList();
            view.SetInfos(infos);
        }
    }

    internal sealed class DrivenValueInfo
    {
        public readonly Component Driver;
        public readonly string Message;
        public readonly bool HasConflict;

        public DrivenValueInfo(Component driver, HashSet<string> members, IEnumerable<(Component Driver, HashSet<string> Members)> allDrivers)
        {
            var others = allDrivers
                .Where(o => o.Driver != driver && (members.Count == 0 || o.Members.Count == 0 || o.Members.Overlaps(members)))
                .Select(o => o.Driver)
                .ToList();

            Driver = driver;
            HasConflict = others.Count > 0;
            Message = $"Driven by {Describe(driver)}";
            if (members.Count > 0)
                Message += $": {string.Join(", ", members)}";
            if (HasConflict)
                Message += $"\nAlso driven by {string.Join(", ", others.Select(Describe))}";
        }

        private static string Describe(Component driver) => $"<b>{ObjectNames.NicifyVariableName(driver.GetType().Name)}</b> on '{driver.name}'";
    }

    internal sealed class DrivenValuesView : IMGUIContainer
    {
        private static readonly Color HOVER_COLOR = new(1, 1, 1, 0.04f);
        private static GUIStyle _marginsStyle;
        private static GUIStyle MarginsStyle => _marginsStyle ??= new GUIStyle
        {
            padding = new RectOffset(EditorStyles.inspectorDefaultMargins.padding.left, 8, 4, 0)
        };

        private List<DrivenValueInfo> _infos = new();

        public DrivenValuesView()
        {
            onGUIHandler = OnGUI;
            tooltip = "Click to ping the driver, double-click to select it";
            style.display = DisplayStyle.None;
            RegisterCallback<PointerMoveEvent>(_ => MarkDirtyRepaint());
            RegisterCallback<PointerLeaveEvent>(_ => MarkDirtyRepaint());
        }

        public void SetInfos(List<DrivenValueInfo> infos)
        {
            if (infos.SequenceEqual(_infos))
                return;

            _infos = infos;
            style.display = infos.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            MarkDirtyRepaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(MarginsStyle);
            foreach (var info in _infos)
                DrawInfo(info);
            EditorGUILayout.EndVertical();
        }

        private static void DrawInfo(DrivenValueInfo info)
        {
            var type = info.HasConflict ? InfoMessageType.Warning : InfoMessageType.Info;
            var icon = new EnhancedInfoBoxAttributeDrawer.BoxIcon(EnhancedInfoBoxAttributeDrawer.GetDefaultIcon(type), EnhancedInfoBoxAttributeDrawer.GetDefaultColor(type));
            EnhancedInfoBoxAttributeDrawer.MessageBox(info.Message, null, icon, false, null, FontAwesomeEditorIcons.ArrowUpRightFromSquareSolid, "Open driver in a floating inspector", out var openClicked);
            var rect = GUILayoutUtility.GetLastRect();

            if (openClicked && info.Driver != null)
                EditorUtility.OpenPropertyEditor(info.Driver);

            var e = Event.current;
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (e.type == EventType.Repaint && rect.Contains(e.mousePosition))
                EditorGUI.DrawRect(rect, HOVER_COLOR);

            if (e.type != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition) || info.Driver == null)
                return;

            if (e.clickCount == 2)
                Selection.activeObject = info.Driver;
            else
                EditorGUIUtility.PingObject(info.Driver);
            e.Use();
        }
    }
}
#endif
