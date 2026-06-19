using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.EditorTools
{
    public class DefinesConfigWindow : OdinEditorWindow
    {
        [Searchable(FilterOptions = SearchFilterOptions.ValueToString)]
        [EnhancedTableList(ShowFoldout = false, ShowPaging = false, OnTitleBarGUI = nameof(OnRecordsToolbarGUI))]
        [SerializeField]
        private List<DefineRecord> _Records;

        public static IEnumerable<SupportedBuildTarget> SupportedBuildTargets => EnumUtils.GetValues<SupportedBuildTarget>();

        [MenuItem("Tools/Defines Config")]
        public static void OpenFontReplaceUtility()
        {
            GetWindow<DefinesConfigWindow>();
        }

        private void OnRecordsToolbarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                RefreshRecords();
            }
        }

        [OnInspectorInit]
        private void RefreshRecords()
        {
            var dict = new Dictionary<string, List<SupportedBuildTarget>>();
            foreach (var target in SupportedBuildTargets)
            {
                var namedTarget = GetNamedTarget(target);
                DefinesUtility.GetScriptingDefineSymbols(namedTarget, out var defines);
                foreach (var define in defines)
                {
                    if (!dict.TryGetValue(define, out var targetGroups))
                    {
                        targetGroups = new List<SupportedBuildTarget>();
                        dict.Add(define, targetGroups);
                    }
                    targetGroups.Add(target);
                }
            }
            _Records = dict.Select(d => new DefineRecord(d.Key, d.Value)).ToList();
        }

        [Button]
        private void ApplyRecords()
        {
            foreach (var target in SupportedBuildTargets)
            {
                var namedTarget = GetNamedTarget(target);
                var defines = _Records.Where(r => r.BuildTargets.Contains(target)).Select(r => r.Define).ToList();
                DefinesUtility.SetScriptingDefineSymbols(namedTarget, defines);
            }
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private static readonly Dictionary<SupportedBuildTarget, Texture> PLATFORM_ICON_CACHE = new();

        // Uses the same built-in icons Unity shows in the Build Profiles / Build Settings window.
        // Falls back to a FontAwesome icon for any platform Unity has no icon for in this editor.
        private static Texture GetPlatformIcon(SupportedBuildTarget buildTarget)
        {
            if (PLATFORM_ICON_CACHE.TryGetValue(buildTarget, out var cached))
                return cached;

            var iconName = GetPlatformIconName(buildTarget);
            var icon = !string.IsNullOrEmpty(iconName) ? EditorGUIUtility.IconContent(iconName)?.image : null;
            icon ??= GetEditorIcon(buildTarget).Highlighted;
            PLATFORM_ICON_CACHE[buildTarget] = icon;
            return icon;
        }

        private static string GetPlatformIconName(SupportedBuildTarget buildTarget)
        {
            return buildTarget switch
            {
                SupportedBuildTarget.Standalone => "BuildSettings.Standalone.Small",
                SupportedBuildTarget.Server => "BuildSettings.DedicatedServer.Small",
                SupportedBuildTarget.Android => "BuildSettings.Android.Small",
                SupportedBuildTarget.IOS => "BuildSettings.iPhone.Small",
                SupportedBuildTarget.WebGL => "BuildSettings.WebGL.Small",
                SupportedBuildTarget.WindowsStore => "BuildSettings.Metro.Small",
                SupportedBuildTarget.PS4 => "BuildSettings.PS4.Small",
                SupportedBuildTarget.PS5 => "BuildSettings.PS5.Small",
                SupportedBuildTarget.XboxOne => "BuildSettings.XboxOne.Small",
                SupportedBuildTarget.TvOS => "BuildSettings.tvOS.Small",
                SupportedBuildTarget.VisionOS => "BuildSettings.VisionOS.Small",
                SupportedBuildTarget.Switch => "BuildSettings.Switch.Small",
                SupportedBuildTarget.EmbeddedLinux => "BuildSettings.EmbeddedLinux.Small",
                _ => null
            };
        }

        private static EditorIcon GetEditorIcon(SupportedBuildTarget buildTarget)
        {
            return buildTarget switch
            {
                SupportedBuildTarget.Standalone => FontAwesomeEditorIcons.DesktopSolid,
                SupportedBuildTarget.Server => FontAwesomeEditorIcons.ServerSolid,
                SupportedBuildTarget.Android => CustomEditorIcons.Android,
                SupportedBuildTarget.IOS => FontAwesomeEditorIcons.Apple,
                SupportedBuildTarget.WebGL => FontAwesomeEditorIcons.GlobeSolid,
                SupportedBuildTarget.WindowsStore => FontAwesomeEditorIcons.Windows,
                SupportedBuildTarget.PS4 => FontAwesomeEditorIcons.Playstation,
                SupportedBuildTarget.PS5 => FontAwesomeEditorIcons.Playstation,
                SupportedBuildTarget.XboxOne => FontAwesomeEditorIcons.Xbox,
                SupportedBuildTarget.TvOS => FontAwesomeEditorIcons.TvSolid,
                SupportedBuildTarget.VisionOS => FontAwesomeEditorIcons.VrCardboardSolid,
                SupportedBuildTarget.Switch => FontAwesomeEditorIcons.GamepadSolid,
                SupportedBuildTarget.EmbeddedLinux => FontAwesomeEditorIcons.MicrochipSolid,
                _ => FontAwesomeEditorIcons.SquareQuestionSolid
            };
        }

        private static NamedBuildTarget GetNamedTarget(SupportedBuildTarget buildTarget)
        {
            return buildTarget switch
            {
                SupportedBuildTarget.Standalone => NamedBuildTarget.Standalone,
                SupportedBuildTarget.Server => NamedBuildTarget.Server,
                SupportedBuildTarget.Android => NamedBuildTarget.Android,
                SupportedBuildTarget.IOS => NamedBuildTarget.iOS,
                SupportedBuildTarget.WebGL => NamedBuildTarget.WebGL,
                SupportedBuildTarget.WindowsStore => NamedBuildTarget.WindowsStoreApps,
                SupportedBuildTarget.PS4 => NamedBuildTarget.PS4,
                SupportedBuildTarget.PS5 => NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.PS5),
                SupportedBuildTarget.XboxOne => NamedBuildTarget.XboxOne,
                SupportedBuildTarget.TvOS => NamedBuildTarget.tvOS,
                SupportedBuildTarget.VisionOS => NamedBuildTarget.VisionOS,
                SupportedBuildTarget.Switch => NamedBuildTarget.NintendoSwitch,
                SupportedBuildTarget.EmbeddedLinux => NamedBuildTarget.EmbeddedLinux,
                _ => NamedBuildTarget.Unknown
            };
        }

        public enum SupportedBuildTarget
        {
            Standalone,
            Server,
            Android,
            IOS,
            WebGL,
            WindowsStore,
            PS4,
            PS5,
            XboxOne,
            TvOS,
            VisionOS,
            Switch,
            EmbeddedLinux
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptIn)]
        public class DefineRecord
        {
            [SerializeField]
            [JsonProperty("Define")]
            private string _Define;

            [FormerlySerializedAs("_BuildTargetGroups")]
            [HideInInspector]
            [SerializeField]
            [JsonProperty("BuildTargets")]
            private List<SupportedBuildTarget> _BuildTargets = new();

            public string Define => _Define;
            public List<SupportedBuildTarget> BuildTargets => _BuildTargets;

            public DefineRecord() { }
            public DefineRecord(string define, List<SupportedBuildTarget> targets)
            {
                _Define = define;
                _BuildTargets = targets;
            }

            private const int ICON_SIZE = 18;
            private const int ICON_SPACING = 2;
            private const int TARGET_COUNT = 13;
            private const int PLATFORMS_COLUMN_WIDTH = ICON_SPACING + TARGET_COUNT * (ICON_SIZE + ICON_SPACING + 3) + ICON_SPACING;

            [EnhancedTableColumn(PLATFORMS_COLUMN_WIDTH)]
            [VerticalGroup("Platforms", Order = -1)]
            [OnInspectorGUI]
            private void DrawBuildTargets(InspectorProperty property)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(ICON_SPACING);
                foreach (var targetGroup in SupportedBuildTargets)
                {
                    var rect = GUILayoutUtility.GetRect(ICON_SIZE, ICON_SIZE, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(ICON_SIZE));
                    var enabled = _BuildTargets.Contains(targetGroup);
                    var icon = GetPlatformIcon(targetGroup);
                    GUIHelper.PushContentColor(enabled ? Color.white : new Color(1f, 1f, 1f, 0.35f));
                    if (SirenixEditorGUI.IconButton(rect, icon, targetGroup.ToString()))
                    {
                        if (enabled) _BuildTargets.Remove(targetGroup);
                        else _BuildTargets.Add(targetGroup);
                        property.MarkSerializationRootDirty();
                    }
                    GUIHelper.PopContentColor();
                    GUILayout.Space(ICON_SPACING);
                }
                GUILayout.EndHorizontal();
            }

            public bool HasBuildTarget(SupportedBuildTarget targetGroup)
            {
                return _BuildTargets.Contains(targetGroup);
            }

            public override string ToString() => Define;
        }
    }
}
