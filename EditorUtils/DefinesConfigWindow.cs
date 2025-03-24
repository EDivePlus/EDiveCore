using System;
using System.Collections.Generic;
using System.Linq;
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

namespace EDIVE.EditorUtils
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

        private static EditorIcon GetEditorIcon(SupportedBuildTarget buildTarget)
        {
            return buildTarget switch
            {
                SupportedBuildTarget.Standalone => FontAwesomeEditorIcons.DesktopSolid,
                SupportedBuildTarget.Server => FontAwesomeEditorIcons.ServerSolid,
                SupportedBuildTarget.Android => CustomEditorIcons.Android,
                SupportedBuildTarget.IOS => FontAwesomeEditorIcons.Apple,
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
                _ => NamedBuildTarget.Unknown
            };
        }

        public enum SupportedBuildTarget
        {
            Standalone,
            Server,
            Android,
            IOS
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

            [EnhancedTableColumn(100)]
            [VerticalGroup("Platforms", Order = -1)]
            [OnInspectorGUI]
            private void DrawBuildTargets(InspectorProperty property)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                foreach (var targetGroup in SupportedBuildTargets)
                {
                    var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
                    var enabled = _BuildTargets.Contains(targetGroup);
                    var editorIcon = GetEditorIcon(targetGroup);
                    var icon = enabled ? editorIcon.Highlighted : editorIcon.Inactive;
                    GUIHelper.PushContentColor(enabled ? ColorTools.Green : ColorTools.White);
                    if (SirenixEditorGUI.IconButton(rect, icon, targetGroup.ToString()))
                    {
                        if (enabled) _BuildTargets.Remove(targetGroup);
                        else _BuildTargets.Add(targetGroup);
                        property.MarkSerializationRootDirty();
                    }
                    GUIHelper.PopContentColor();
                    GUILayout.Space(2);
                }
                GUILayout.Space(2);
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
