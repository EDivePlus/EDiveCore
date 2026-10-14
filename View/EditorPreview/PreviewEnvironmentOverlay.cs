// Author: František Holubec
// Created: 14.10.2026

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.View.EditorPreview
{
    // Scene view panel for the Prefab Mode preview environment
    [Overlay(typeof(SceneView), "edive-ui-preview", "UI Preview")]
    public class PreviewEnvironmentOverlay : Overlay, ITransientOverlay
    {
        private const float LABEL_WIDTH = 48;

        private DropdownField _preset;
        private DropdownField _area;
        private Label _activeName;
        private Label _activeMode;
        private Button _select;

        public bool visible => PreviewEnvironment.IsAvailable;

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement { style = { minWidth = 220 } };

            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4, marginLeft = 3 } };
            _activeName = new Label { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _activeMode = new Label { style = { flexGrow = 1, opacity = 0.6f, marginLeft = 4 } };
            _select = new Button(SelectActivePreset)
            {
                tooltip = "Select preset asset",
                style =
                {
                    width = 20,
                    height = 18,
                    paddingLeft = 0,
                    paddingRight = 0,
                    backgroundImage = (Texture2D) EditorGUIUtility.IconContent("d_ScriptableObject Icon").image,
                    backgroundSize = new BackgroundSize(BackgroundSizeType.Contain)
                }
            };
            header.Add(_activeName);
            header.Add(_activeMode);
            header.Add(_select);
            root.Add(header);

            _preset = CreateDropdown("Preset", "Off, Auto from the simulated device, or a fixed preset");
            _preset.RegisterValueChangedCallback(e => PreviewEnvironment.PresetSelection = e.newValue);
            root.Add(_preset);

            _area = CreateDropdown("Area", "Part of the screen the prefab is laid out in");
            _area.RegisterValueChangedCallback(e => PreviewEnvironment.AreaSelection = e.newValue);
            root.Add(_area);

            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                PreviewEnvironment.Changed += Refresh;
                Refresh();
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ => PreviewEnvironment.Changed -= Refresh);
            return root;
        }

        private static DropdownField CreateDropdown(string label, string tooltip)
        {
            var dropdown = new DropdownField(label) { tooltip = tooltip };
            dropdown.labelElement.style.minWidth = LABEL_WIDTH;
            dropdown.labelElement.style.width = LABEL_WIDTH;
            return dropdown;
        }

        private void Refresh()
        {
            var selection = PreviewEnvironment.PresetSelection;
            var choices = new List<string> { PreviewEnvironment.OFF, PreviewEnvironment.AUTO };
            choices.AddRange(PreviewEnvironment.Presets.Where(p => p != null).Select(p => p.name));
            _preset.choices = choices;
            _preset.SetValueWithoutNotify(selection);

            var active = PreviewEnvironment.ActivePreset;
            _area.choices = active != null ? active.Areas.Where(a => a != null).Select(a => a.Name).ToList() : new List<string>();
            _area.SetValueWithoutNotify(PreviewEnvironment.ActiveArea?.Name ?? string.Empty);
            _area.SetEnabled(active != null);

            _activeName.text = active != null ? active.name : selection == PreviewEnvironment.OFF ? "Off" : "No preset";
            _activeMode.text = active != null && selection == PreviewEnvironment.AUTO ? "auto" : string.Empty;
            _select.SetEnabled(active != null);
        }

        private static void SelectActivePreset()
        {
            if (PreviewEnvironment.ActivePreset != null)
                Selection.activeObject = PreviewEnvironment.ActivePreset;
        }
    }
}
