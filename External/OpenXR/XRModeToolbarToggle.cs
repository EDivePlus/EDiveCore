// Author: František Holubec
// Created: 11.09.2026

#if UNITY_6000_3_OR_NEWER && XR_INTERACTION_TOOLKIT
using System;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions;
using EDIVE.XRTools.DeviceSimulator;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;

namespace EDIVE.External.OpenXR
{
    public static class XRModeToolbarToggle
    {
        private enum XRMode
        {
            Desktop,
            Simulator,
            Headset
        }

        private static readonly int MODE_COUNT = Enum.GetValues(typeof(XRMode)).Length;

        private static XRGeneralSettings XRSettings => XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

        private static XRMode Mode
        {
            get
            {
                if (XRDeviceSimulatorUtils.AutoInstantiateSimulator)
                    return XRMode.Simulator;
                return XRSettings != null && XRSettings.InitManagerOnStart ? XRMode.Headset : XRMode.Desktop;
            }
            set
            {
                XRDeviceSimulatorUtils.AutoInstantiateSimulator = value == XRMode.Simulator;
                if (XRSettings != null)
                    XRSettings.InitManagerOnStart = value == XRMode.Headset;
            }
        }

        [MainToolbarElement("EDive/XR Mode", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 15)]
        public static MainToolbarElement CreateToolbarButton()
        {
            return MainToolbarUtility.CreateElement(() =>
            {
                var button = new EditorToolbarButton();
                button.AddToClassList("unity-editor-toolbar-element");
                button.clicked += () =>
                {
                    Mode = (XRMode) (((int) Mode + 1) % MODE_COUNT);
                    UpdateVisual();
                };

                button.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                EditorApplication.playModeStateChanged += _ =>
                {
                    button.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                };
                UpdateVisual();
                return button;

                void UpdateVisual()
                {
                    var mode = Mode;
                    button.tooltip = $"XR Mode: {mode}\nClick: next";
                    button.icon = mode switch
                    {
                        XRMode.Simulator => FontAwesomeEditorIcons.KeyboardSolid.Raw,
                        XRMode.Headset => FontAwesomeEditorIcons.VrCardboardSolid.Raw,
                        _ => FontAwesomeEditorIcons.DesktopSolid.Raw
                    };
                }
            });
        }
    }
}
#endif
