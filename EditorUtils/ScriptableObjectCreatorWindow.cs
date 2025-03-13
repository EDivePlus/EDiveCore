using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EDIVE.External.ToolbarExtensions;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorUtils
{
    public class ScriptableObjectCreatorWindow : OdinMenuEditorWindow
    {
        private static readonly HashSet<Type> SCRIPTABLE_OBJECT_TYPES = AssemblyUtilities.GetTypes(AssemblyCategory.ProjectSpecific)
            .Where(t => t.IsClass && typeof(ScriptableObject).IsAssignableFrom(t) && !typeof(EditorWindow).IsAssignableFrom(t) && !typeof(UnityEditor.Editor).IsAssignableFrom(t))
            .ToHashSet();


        private ScriptableObject _previewObject;
        private Vector2 _scroll;

        private static EditorIcon MainIcon => FontAwesomeEditorIcons.FolderPlusSolid;

        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            ToolbarExtender.AddToRightToolbar(OnToolbarGUI, 1000);
        }

        private static void OnToolbarGUI()
        {
            GUILayout.Space(2);
            if (GUILayout.Button(new GUIContent(null, MainIcon.Highlighted, "Create Scriptable Object"), ToolbarStyles.ToolbarButton, GUILayout.Width(30)))
            {
                OpenWindow();
            }

            GUILayout.Space(2);
        }

        [MenuItem("Assets/Create Scriptable Object", priority = -1000)]
        public static void OpenWindow()
        {
            var window = GetWindow<ScriptableObjectCreatorWindow>();
            window.SetupWindowStyle();
        }

        protected override void Initialize()
        {
            base.Initialize();
            SetupWindowStyle();
        }

        private void SetupWindowStyle() { titleContent = new GUIContent("Scriptable Object Creator", MainIcon.Highlighted); }

        protected override OdinMenuTree BuildMenuTree()
        {
            MenuWidth = 270;
            WindowPadding = Vector4.zero;

            var tree = new OdinMenuTree(false);
            tree.AddRange(SCRIPTABLE_OBJECT_TYPES.Where(x => !x.IsAbstract), GetMenuPathForType).AddThumbnailIcons();
            tree.SortMenuItemsByName();

            tree.Config.DrawSearchToolbar = true;
            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;
            tree.Selection.SelectionConfirmed += _ => CreateAssetAtCurrentPath();
            tree.Selection.SelectionChanged += changedType =>
            {
                if (_previewObject && !AssetDatabase.Contains(_previewObject))
                {
                    DestroyImmediate(_previewObject);
                }

                if (changedType != SelectionChangedType.ItemAdded)
                {
                    return;
                }

                if (MenuTree.Selection.LastOrDefault()?.Value is Type t && !t.IsAbstract)
                {
                    _previewObject = CreateInstance(t);
                }
            };

            return tree;
        }

        private static string GetMenuPathForType(Type type)
        {
            var nameBuilder = new StringBuilder();
            while (type != null && SCRIPTABLE_OBJECT_TYPES.Contains(type))
            {
                nameBuilder.Insert(0, "/");
                nameBuilder.Insert(0, type.Name.Split('`').First().SplitPascalCase());
                type = type.BaseType;
            }

            return nameBuilder.ToString();
        }

        protected override IEnumerable<object> GetTargets() { yield return _previewObject; }

        protected override void DrawEditor(int index)
        {
            _scroll = GUILayout.BeginScrollView(_scroll);
            {
                base.DrawEditor(index);
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            GUIHelper.PushGUIEnabled(_previewObject != null);
            SirenixEditorGUI.HorizontalLineSeparator();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(GUIHelper.TempContent(" Create At Root", FontAwesomeEditorIcons.TreeSolid.Highlighted), GUILayoutOptions.Height(30)))
            {
                EditorApplication.delayCall += CreateAssetAtRoot;
            }

            if (GUILayout.Button(GUIHelper.TempContent(" Create With Dialog", FontAwesomeEditorIcons.FolderSolid.Highlighted), GUILayoutOptions.Height(30)))
            {
                EditorApplication.delayCall += CreateAssetWithDialog;
            }

            if (GUILayout.Button(GUIHelper.TempContent(" Create At Current Path", FontAwesomeEditorIcons.FolderUserSolid.Highlighted), GUILayoutOptions.Height(30)))
            {
                EditorApplication.delayCall += CreateAssetAtCurrentPath;
            }

            GUILayout.EndHorizontal();
            GUIHelper.PopGUIEnabled();
        }

        private void CreateAssetAtPath(string path)
        {
            if (!_previewObject) return;

            var dest = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(_previewObject, dest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(this);
            Selection.activeObject = _previewObject;
        }

        private void CreateAssetAtRoot()
        {
            CreateAssetAtPath($"Assets/{NewFileName}");
        }

        private void CreateAssetWithDialog()
        {
            var path = EditorUtility.SaveFilePanel($"Create new {SelectedClassName}", Application.dataPath, $"{NewFileName}", "asset");
            var localPath = PathUtility.GetProjectRelativePath(path);
            if (string.IsNullOrEmpty(localPath))
                return;

            CreateAssetAtPath(localPath);
        }

        private void CreateAssetAtCurrentPath()
        {
            var tryGetActiveFolderPath = typeof(ProjectWindowUtil).GetMethod("TryGetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            var args = new object[] {null};

            if (tryGetActiveFolderPath == null)
                return;

            var found = (bool) tryGetActiveFolderPath.Invoke(null, args);
            if (!found)
                return;

            var path = (string) args[0];
            if (string.IsNullOrEmpty(path))
                return;

            CreateAssetAtPath($"{path}/{NewFileName}");
        }

        private string SelectedClassName => ((Type) MenuTree.Selection.First().Value).Name;
        private string NewFileName => $"new {SelectedClassName}.asset";
    }
}