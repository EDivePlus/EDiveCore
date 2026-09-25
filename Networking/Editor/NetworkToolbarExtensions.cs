// Author: František Holubec
// Created: 31.03.2025

#if UNITY_EDITOR && UNITY_6000_3_OR_NEWER
using Sirenix.OdinInspector.Editor;
using UnityEditor;

using System.Linq;
using Sirenix.Utilities;
using UnityEditor.Toolbars;
using EDIVE.OdinExtensions;
using EDIVE.EditorUtils;

namespace EDIVE.Networking.Utils
{
    public static class NetworkToolbarExtensions
    {
        [MainToolbarElement("EDive/Network State", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -5)]
        public static MainToolbarElement CreatePlayRootSceneButton()
        {
            return MainToolbarUtility.CreateElement(() =>
            {
                var dropdown = new EditorToolbarDropdown
                {
                    icon = FontAwesomeEditorIcons.GlobeSolid.Raw,
                    tooltip = "Network State"
                };
                dropdown.AddToClassList("unity-editor-toolbar-element");
                dropdown.style.marginLeft = 10;
                dropdown.style.marginRight = 10;
                dropdown.clicked += () =>
                {
                    var selector = new EnumSelector<NetworkRuntimeMode>();
                    selector.EnableSingleClickToSelect();
                    selector.SetSelection(NetworkUtils.EditorRuntimeMode);
                    selector.SelectionConfirmed += selection =>
                    {
                        if (selection.Any())
                        {
                            NetworkUtils.EditorRuntimeMode = selection.First();
                            UpdateLabel();
                        }
                    };
                    selector.ShowInPopup(dropdown.worldBound.MinWidth(200));
                };
                
                dropdown.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                // Unsub when toolbar element rebuilt
                dropdown.RegisterCallback<UnityEngine.UIElements.AttachToPanelEvent>(_ =>
                {
                    EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                    EditorApplication.playModeStateChanged += OnPlayModeChanged;
                });
                dropdown.RegisterCallback<UnityEngine.UIElements.DetachFromPanelEvent>(_ => EditorApplication.playModeStateChanged -= OnPlayModeChanged);
                UpdateLabel();
                return dropdown;

                void OnPlayModeChanged(PlayModeStateChange _)
                {
                    dropdown.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                }

                void UpdateLabel()
                {
                    dropdown.text = NetworkUtils.EditorRuntimeMode.ToString();
                }
            });
        }
    }
}
#endif
