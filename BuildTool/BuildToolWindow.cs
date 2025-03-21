using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Presets;
using EDIVE.EditorUtils;
using EDIVE.External.ToolbarExtensions;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool
{
    public class BuildToolWindow : OdinMenuEditorWindow
    {
        private static BuildToolWindow CurrentInstance { get; set; }

        private static readonly BuildPanel BUILD_PANEL = new();
        private static readonly UserConfigsPanel USER_CONFIGS_PANEL = new();

        private static EditorIcon BuildHelperIcon => FontAwesomeEditorIcons.HammerSolid;
        
        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            ToolbarExtender.AddToLeftToolbar(OnToolbarGUI, -400);
        }

        private static void OnToolbarGUI()
        {
            GUILayout.Space(2);
            if (GUILayout.Button(new GUIContent(null, BuildHelperIcon.Highlighted, "Build Tool"), ToolbarStyles.ToolbarButton, GUILayout.Width(30)))
            {
                OpenWindow();
            }
            GUILayout.Space(2);
        }

        [MenuItem("Tools/Build Tool %g", priority = 120)]
        public static void OpenWindow()
        {
            var window = GetWindow<BuildToolWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(600, 800);
            window.SetupWindowStyle();
            window.TrySelectMenuItemWithObject(BUILD_PANEL);
        }

        protected override void Initialize()
        {
            base.Initialize();
            SetupWindowStyle();
            ForceMenuTreeRebuild();
            CurrentInstance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            CurrentInstance = null;
        }

        private void SetupWindowStyle()
        {
            titleContent = new GUIContent("Build Helper", BuildHelperIcon.Highlighted);
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(true)
            {
                {"Build", BUILD_PANEL, BuildHelperIcon},
                {"Global Settings", BuildGlobalSettings.Instance, FontAwesomeEditorIcons.GearSolid},
                {"User Configs", USER_CONFIGS_PANEL, FontAwesomeEditorIcons.UserGroupSolid},
            };
            tree.DefaultMenuStyle = new OdinMenuStyle();
            return tree;
        }

        [Serializable]
        public class BuildPanel
        {
            [SerializeReference]
            [EnhancedTableList(AlwaysExpanded = true, IsReadOnly = true, OnTitleBarGUI = nameof(OnPresetListTitleBarGUI))]
            public List<ABuildPreset> _Presets;

            [OnInspectorInit]
            private void Initialize()
            {
                RefreshPresets();
            }

            private void RefreshPresets()
            {
                _Presets = EditorAssetUtils.FindAllAssetsOfType<ABuildPlatformConfig>().Select(c => c.CreatePreset(BuildGlobalSettings.Instance.CurrentUser)).ToList();
            }

            [PropertyOrder(-1)]
            [Button("Player Settings")]
            private void OpenPlayerSettings()
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }

            private void OnPresetListTitleBarGUI()
            {
                if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
                {
                    RefreshPresets();
                }
            }
        }

        [Serializable]
        public class UserConfigsPanel
        {
            [SerializeField]
            [UsedImplicitly]
            [EnhancedAssetList(AutoPopulate = true)]
            private List<BuildUserConfig> _UserConfigs;
        }
    }
}
