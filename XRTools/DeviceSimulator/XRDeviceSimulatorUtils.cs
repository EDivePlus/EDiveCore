// Author: František Holubec
// Created: 15.05.2025

#if UNITY_6000_3_OR_NEWER
#define UNITY_6_TOOLBAR
#endif

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using EDIVE.OdinExtensions;

#if UNITY_EDITOR
#if UNITY_6_TOOLBAR
using EDIVE.EditorUtils;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;
#else
using UnityEditor;
using Sirenix.Utilities.Editor;
using EDIVE.External.ToolbarExtensions;
#endif
#endif



namespace EDIVE.XRTools.DeviceSimulator
{
    public static class XRDeviceSimulatorUtils
    {
        public static bool SimulatorEnabled => XRDeviceSimulatorSettings.Instance.automaticallyInstantiateSimulatorPrefab &&
                                               (!XRDeviceSimulatorSettings.Instance.automaticallyInstantiateInEditorOnly || Application.isEditor);

        // This method is used to fix the issue with the XRDeviceSimulator not being instantiated
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad), Preserve]
        public static void Initialize()
        {
            if (!SimulatorEnabled)
                return;

            if (XRInteractionSimulator.instance || XRDeviceSimulator.instance)
                return;

            var simulatorPrefab = XRDeviceSimulatorSettings.Instance.simulatorPrefab;
            if (!simulatorPrefab)
                return;

            var simulatorInstance = Object.Instantiate(simulatorPrefab);
            simulatorInstance.name = simulatorPrefab.name;
            Object.DontDestroyOnLoad(simulatorInstance);
        }

#if UNITY_EDITOR
#if UNITY_6_TOOLBAR
        [MainToolbarElement("EDive/XRDeviceSimulator Toggle", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 15)]
        public static MainToolbarElement CreateToolbarButton()
        {
            return MainToolbarUtility.CreateElement(() =>
            {
                var toggle = new EditorToolbarToggle();
                toggle.AddToClassList("unity-editor-toolbar-element");

                void UpdateVisual(bool value)
                {
                    toggle.icon = value
                        ? FontAwesomeEditorIcons.CheckToSlotSolid.Raw
                        : FontAwesomeEditorIcons.XmarkToSlotSolid.Raw;

                    toggle.tooltip = value
                        ? "Disable Device Simulator"
                        : "Enable Device Simulator";
                }

                var settings = XRDeviceSimulatorSettings.Instance;

                toggle.SetValueWithoutNotify(settings.automaticallyInstantiateSimulatorPrefab);
                UpdateVisual(toggle.value);

                toggle.RegisterValueChangedCallback(evt =>
                {
                    settings.automaticallyInstantiateSimulatorPrefab = evt.newValue;
                    UpdateVisual(evt.newValue);
                });

                return toggle;
            });
        }
#else
        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            XRDeviceSimulatorSettings.Instance.hideFlags = HideFlags.DontSaveInEditor;
            ToolbarExtender.AddToRightToolbar(OnToolbarGUI, -90);
        }

        private static void OnToolbarGUI()
        {
            GUILayout.Space(2);
            var enabled = XRDeviceSimulatorSettings.Instance.automaticallyInstantiateSimulatorPrefab;
            var icon = enabled ? FontAwesomeEditorIcons.CheckToSlotSolid : FontAwesomeEditorIcons.XmarkToSlotSolid;
            var tooltip = enabled ? "Disable Device Simulator" : "Enable Device Simulator";

            if (GUILayout.Button(GUIHelper.TempContent(icon.Highlighted, tooltip), ToolbarStyles.ToolbarButtonBiggerIcon, GUILayout.Width(30)))
            {
                XRDeviceSimulatorSettings.Instance.automaticallyInstantiateSimulatorPrefab = !enabled;
            }
            GUILayout.Space(2);
        }
#endif
#endif

    }
}
