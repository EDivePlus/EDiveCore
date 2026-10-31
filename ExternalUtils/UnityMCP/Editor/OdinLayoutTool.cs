// Author: František Holubec
// Created: 31.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.UnityMCP.Editor
{
    [McpForUnityTool("odin_layout",
        Description =
            "Measures the Odin inspector layout of the selection (or a target): per property row rect, value column x, right edge, label width and indent, " +
            "plus value columns grouped to spot misaligned rows. Coordinates are relative to the inspector; compare rows with each other.")]
    public static class OdinLayoutTool
    {
        public class Parameters
        {
            [ToolParameter("Asset path or scene hierarchy path to select first. Default: current selection.", Required = false)]
            public string Target { get; set; }

            [ToolParameter("Only editors whose target type name contains this text (e.g. LocalizeStringEvent).", Required = false)]
            public string Component { get; set; }

            [ToolParameter("Only properties whose path contains this text.", Required = false)]
            public string Contains { get; set; }

            [ToolParameter("Include group and collection rows, not only fields.", Required = false, DefaultValue = "false")]
            public bool IncludeContainers { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            var target = ToolParams.GetString(@params, "Target");
            if (!string.IsNullOrEmpty(target) && !Select(target))
                return new ErrorResponse($"Nothing found at '{target}'.");

            var window = CaptureWindowTool.FindWindow("InspectorWindow");
            if (window == null)
                return new ErrorResponse("No Inspector window is open.");

            LayoutProbeDrawer.Begin();
            try
            {
                ActiveEditorTracker.sharedTracker.ForceRebuild();
                // Repaint-only values lag a frame
                CaptureWindowTool.RepaintImmediately(window, 3);
                return new SuccessResponse("Layout", Collect(@params));
            }
            finally
            {
                LayoutProbeDrawer.End();
                ActiveEditorTracker.sharedTracker.ForceRebuild();
                window.Repaint();
            }
        }

        private static bool Select(string target)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(target);
            var selected = asset != null ? asset : GameObject.Find(target);
            if (selected == null)
                return false;
            Selection.activeObject = selected;
            return true;
        }

        private static object Collect(JObject @params)
        {
            var component = ToolParams.GetString(@params, "Component");
            var contains = ToolParams.GetString(@params, "Contains");
            var includeContainers = ToolParams.GetBool(@params, "IncludeContainers", false);

            var editors = new List<object>();
            foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors.OfType<OdinEditor>())
            {
                var typeName = editor.target.GetType().Name;
                if (!string.IsNullOrEmpty(component) && typeName.IndexOf(component, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var rows = new List<Row>();
                foreach (var property in editor.Tree.EnumerateTree(true, true))
                {
                    if (!LayoutProbeDrawer.DATA.TryGetValue(property, out var data))
                        continue;
                    if (!includeContainers && (property.Info.PropertyType == PropertyType.Group || property.ChildResolver is ICollectionResolver))
                        continue;
                    if (!string.IsNullOrEmpty(contains) && property.Path.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    rows.Add(new Row(property.Path, data));
                }

                editors.Add(new
                {
                    type = typeName,
                    columns = rows.Where(r => r.Label).GroupBy(r => r.ValueX).OrderBy(g => g.Key)
                        .Select(g => new {valueX = g.Key, count = g.Count(), sample = g.Take(3).Select(r => r.Path)}),
                    rows
                });
            }

            return new {editors};
        }

        [UsedImplicitly(ImplicitUseTargetFlags.Members)]
        private class Row
        {
            public string Path;
            public int X, Y, Width, Height, ValueX, Right, LabelWidth, Indent;
            public bool Label;

            public Row(string path, LayoutProbeDrawer.Measure measure)
            {
                Path = path;
                X = Mathf.RoundToInt(measure.Rect.x);
                Y = Mathf.RoundToInt(measure.Rect.y);
                Width = Mathf.RoundToInt(measure.Rect.width);
                Height = Mathf.RoundToInt(measure.Rect.height);
                Right = Mathf.RoundToInt(measure.Rect.xMax);
                LabelWidth = Mathf.RoundToInt(measure.LabelWidth);
                Indent = measure.Indent;
                Label = measure.HasLabel;
                ValueX = Label ? Mathf.RoundToInt(measure.Rect.x + measure.LabelWidth) : X;
            }
        }
    }

    // Records each property's drawn rect and label context, only while odin_layout runs
    [DrawerPriority(0, 0, 2)]
    public class LayoutProbeDrawer : OdinDrawer
    {
        public struct Measure
        {
            public Rect Rect;
            public float LabelWidth;
            public int Indent;
            public bool HasLabel;
        }

        private static bool _enabled;

        public static readonly Dictionary<InspectorProperty, Measure> DATA = new();

        public static void Begin()
        {
            DATA.Clear();
            _enabled = true;
        }

        public static void End() => _enabled = false;

        public override bool CanDrawProperty(InspectorProperty property) => _enabled;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (!_enabled || Event.current.type != EventType.Repaint)
            {
                CallNextDrawer(label);
                return;
            }

            var labelWidth = GUIHelper.BetterLabelWidth;
            var indent = EditorGUI.indentLevel;
            GUIHelper.BeginLayoutMeasuring();
            CallNextDrawer(label);
            DATA[Property] = new Measure {Rect = GUIHelper.EndLayoutMeasuring(), LabelWidth = labelWidth, Indent = indent, HasLabel = label != null};
        }
    }
}
