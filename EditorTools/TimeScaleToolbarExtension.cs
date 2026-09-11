// Author: František Holubec
// Created: 11.09.2026

#if UNITY_6000_3_OR_NEWER
using System;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.EditorTools
{
    public static class TimeScaleToolbarExtension
    {
        private const float MIN = 0.1f;
        private const float MAX = 10f;
        private static readonly float[] PRESETS = { 0.1f, 0.3f, 0.5f, 1f, 2f, 4f, 10f };
        private static readonly Color MODIFIED_COLOR = new(1f, 0.75f, 0.3f);

        private const string SESSION_SCALE = "EDIVE.TimeScale.Value";
        private const string SESSION_DEFAULT = "EDIVE.TimeScale.Default";

        private static float Scale
        {
            get => Mathf.Clamp(SessionState.GetFloat(SESSION_SCALE, 1f), MIN, MAX);
            set
            {
                value = Mathf.Clamp(value, MIN, MAX);
                SessionState.SetFloat(SESSION_SCALE, value);
                if (EditorApplication.isPlaying)
                    Time.timeScale = value;
            }
        }

        private static float CurrentScale => EditorApplication.isPlaying ? Time.timeScale : Scale;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    SessionState.SetFloat(SESSION_DEFAULT, Time.timeScale);
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    if (!Mathf.Approximately(Scale, 1f))
                        Time.timeScale = Scale;
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    Time.timeScale = SessionState.GetFloat(SESSION_DEFAULT, 1f);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        [MainToolbarElement("EDive/Time Scale", defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = 90)]
        public static MainToolbarElement CreateToolbarElement()
        {
            return MainToolbarUtility.CreateElement(() =>
            {
                var root = new VisualElement
                {
                    tooltip = $"Time Scale ({MIN} - {MAX})",
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center
                    }
                };

                var icon = new Image
                {
                    image = FontAwesomeEditorIcons.GaugeHighSolid.Raw,
                    tooltip = "Reset to 1x",
                    style = { width = 14, height = 14, marginLeft = 4, marginRight = 2 }
                };
                root.Add(icon);

                var field = new FloatField
                {
                    isDelayed = true,
                    style = { width = 35, marginLeft = 0, marginRight = 0 }
                };
                field.RegisterValueChangedCallback(evt =>
                {
                    Scale = evt.newValue;
                    Refresh();
                });
                root.Add(field);

                icon.AddManipulator(new Clickable(() =>
                {
                    Scale = 1f;
                    Refresh();
                }));

                var presets = new EditorToolbarDropdown
                {
                    tooltip = "Presets",
                    style = { width = 12, minWidth = 0, paddingLeft = 0, paddingRight = 0 }
                };
                presets.AddToClassList("unity-editor-toolbar-element");
                var arrow = presets.Q(className: "unity-icon-arrow");
                if (arrow != null)
                {
                    arrow.style.marginLeft = 0;
                    arrow.style.marginRight = 0;
                }
                presets.clicked += () =>
                {
                    var menu = new GenericMenu();
                    foreach (var preset in PRESETS)
                    {
                        var value = preset;
                        menu.AddItem(new GUIContent($"{value:0.##}x"), Mathf.Approximately(CurrentScale, value), () =>
                        {
                            Scale = value;
                            Refresh();
                        });
                    }
                    menu.ShowAsContext();
                };
                root.Add(presets);

                root.schedule.Execute(Refresh).Every(100);
                Refresh();
                return root;

                void Refresh()
                {
                    if (field.focusController?.focusedElement is VisualElement focused && field.Contains(focused))
                        return;

                    var current = CurrentScale;
                    if (!Mathf.Approximately(field.value, current))
                        field.SetValueWithoutNotify(current);

                    var input = field.Q(className: FloatField.inputUssClassName);
                    if (input != null)
                        input.style.color = Mathf.Approximately(current, 1f) ? new StyleColor(StyleKeyword.Null) : MODIFIED_COLOR;
                }
            });
        }
    }
}
#endif
