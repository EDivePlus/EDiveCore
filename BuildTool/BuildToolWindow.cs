#if UNITY_6000_3_OR_NEWER
#define UNITY_6_TOOLBAR
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.ApplicationConfigs;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_6_TOOLBAR
using UnityEditor.Toolbars;
#else
using EDIVE.External.ToolbarExtensions;
#endif

namespace EDIVE.BuildTool
{
    public class BuildToolWindow : OdinMenuEditorWindow
    {
        private static BuildToolWindow CurrentInstance { get; set; }

        private static readonly BuildPanel BUILD_PANEL = new();
        private static readonly PlatformConfigsPanel PLATFORM_CONFIGS_PANEL = new();
        private static readonly UserConfigsPanel USER_CONFIGS_PANEL = new();
        private static readonly AppConfigsPanel APP_CONFIGS_PANEL = new();

        private static EditorIcon BuildToolIcon => FontAwesomeEditorIcons.HammerSolid;
        
#if UNITY_6_TOOLBAR
        [MainToolbarElement("EDive/Build Tool", defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement CreateBuildToolButton()
        {
            return new MainToolbarButton(new MainToolbarContent(BuildToolIcon.Raw, "Build Tool"), OpenWindow);
        }
#else
        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            ToolbarExtender.AddToLeftToolbar(OnToolbarGUI, -400);
        }

        private static void OnToolbarGUI()
        {
            GUILayout.Space(2);
            if (GUILayout.Button(new GUIContent(null, BuildToolIcon.Highlighted, "Build Tool"), ToolbarStyles.ToolbarButton, GUILayout.Width(30)))
            {
                OpenWindow();
            }
            GUILayout.Space(2);
        }
#endif
        
        [MenuItem("Tools/Build Tool %g", priority = 120)]
        public static void OpenWindow()
        {
            var window = GetWindow<BuildToolWindow>();
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
            titleContent = new GUIContent("Build Helper", BuildToolIcon.Highlighted);
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(true)
            {
                {"Build", BUILD_PANEL, BuildToolIcon},
                {"Global Settings", BuildGlobalSettings.Instance, FontAwesomeEditorIcons.GearSolid},
                {"Platform Configs", PLATFORM_CONFIGS_PANEL, FontAwesomeEditorIcons.LaptopMobileSolid},
                {"User Configs", USER_CONFIGS_PANEL, FontAwesomeEditorIcons.UserGroupSolid},
                {"App Configs", APP_CONFIGS_PANEL, FontAwesomeEditorIcons.BrowserSolid},
            };
            tree.DefaultMenuStyle = new OdinMenuStyle();
            return tree;
        }

        [Serializable]
        public class BuildPanel
        {
            [ShowInInspector]
            [InlineIconButton(FontAwesomeEditorIconType.RotateLeftSolid, nameof(ResetUserToDefault))]
            public BuildUserConfig CurrentUser
            {
                get => BuildGlobalSettings.Instance.CurrentUser;
                set => BuildGlobalSettings.Instance.CurrentUser = value;
            }

            [PropertyOrder(10)]
            [SerializeReference]
            [EnhancedTableList(ShowFoldout = false, IsReadOnly = true, OnTitleBarGUI = nameof(OnPresetListTitleBarGUI))]
            public List<BuildPreset> _Presets;

            [OnInspectorInit]
            private void Initialize()
            {
                RefreshPresets();
            }

            private void RefreshPresets()
            {
                _Presets = EditorAssetUtils.FindAllAssetsOfType<BuildPlatformConfig>()
                    .Select(c => new BuildPreset(BuildGlobalSettings.Instance.CurrentUser, c)).ToList();
            }

            [PropertyOrder(-1)]
            [PropertySpace(0, 8)]
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

            private void ResetUserToDefault()
            {
                CurrentUser = null;
            }
        }
        
        [Serializable]
        public abstract class AssetListPanel<TElement> where TElement : Object
        {
            [HorizontalGroup("Collection", 280)]
            [ShowInInspector]
            [EnableGUI]
            [UsedImplicitly]
            [LabelText("@this.ElementsLabel")]
            [ListItemSelector(nameof(SetSelected))]
            [ListDrawerSettings(ShowFoldout = false, IsReadOnly = true)]
            private List<TElement> Elements => AssetsFinder.CurrentElements;

            [HorizontalGroup("Collection")]
            [ShowInInspector]
            [InfoBox("No element selected!", VisibleIf = "@this.Selected == null")]
            [InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
            [CustomValueDrawer(nameof(CustomSelectedElementDrawer))]
            private TElement Selected { get; set; }
            
            private Vector2 _scrollPosition;
            
            private AsyncAssetsFinder<TElement> _assetsFinder;
            private AsyncAssetsFinder<TElement> AssetsFinder => _assetsFinder ??= new AsyncAssetsFinder<TElement>();
            
            private void SetSelected(int index)
            {
                Selected = index >= 0 && index < Elements.Count ? Elements[index] : null;
            }
            
            [OnInspectorInit]
            protected virtual void OnInspectorInit() => AssetsFinder.SearchAssetsAsync();
            
            [UsedImplicitly]
            protected virtual string ElementsLabel => typeof(TElement).Name + "s";

            private TElement CustomSelectedElementDrawer(TElement value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                callNextDrawer.Invoke(label);
                GUILayout.EndScrollView();
                return value;
            }
        }
        
        [Serializable]
        public class PlatformConfigsPanel : AssetListPanel<BuildPlatformConfig>
        {
            protected override string ElementsLabel => "Platform Configs";
        }
        
        [Serializable]
        public class UserConfigsPanel : AssetListPanel<BuildUserConfig>
        {
            protected override string ElementsLabel => "User Configs";
        }
        
        [Serializable]
        public class AppConfigsPanel : AssetListPanel<ApplicationConfig>
        {
            protected override string ElementsLabel => "App Configs";
        }
    }
}
