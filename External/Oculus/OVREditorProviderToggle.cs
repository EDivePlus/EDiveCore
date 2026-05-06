// Author: Michal Petr
// Created: 06.05.2026

using EDIVE.EditorUtils;
using EDIVE.OdinExtensions;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.XR.Management;
using UnityEngine.UIElements;
using UnityEngine.XR;
#if UNITY_EDITOR

namespace EDIVE.External.Oculus
{
    public static class OVREditorProviderToggle
    {
#if UNITY_6000_3_OR_NEWER
        [MainToolbarElement("EDive/XRStandaloneProvider Toggle", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 15)]
        public static MainToolbarElement CreateToolbarButton()
        {
            return MainToolbarUtility.CreateElement(() =>
            {
                var toggle = new EditorToolbarToggle();
                toggle.AddToClassList("unity-editor-toolbar-element");

                var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

                toggle.SetValueWithoutNotify(settings.InitManagerOnStart);
                UpdateVisual(toggle.value);

                toggle.RegisterValueChangedCallback(evt =>
                {
                    settings.InitManagerOnStart = evt.newValue;
                    UpdateVisual(evt.newValue);
                });

                return toggle;

                void UpdateVisual(bool value)
                {
                    toggle.icon = value
                        ? FontAwesomeEditorIcons.VrCardboardSolid.Raw
                        : FontAwesomeEditorIcons.DesktopSolid.Raw;

                    toggle.tooltip = value
                        ? "Disable XR Standalone Provider"
                        : "Enable XR Standalone Provider";
                }
            });
        }
#endif
    }
}

#endif