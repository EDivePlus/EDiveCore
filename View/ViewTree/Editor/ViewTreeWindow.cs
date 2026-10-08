// Author: František Holubec
// Created: 08.10.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.View.ViewTree.Editor
{
    public class ViewTreeWindow : OdinMenuEditorWindow
    {
        private static readonly Color ERROR_COLOR = new(1f, 0.35f, 0.35f);
        private static readonly Color FIX_COLOR = new(0.5f, 1f, 0.5f);
        private static readonly Color TYPE_COLOR = new(0.4f, 0.9f, 1f);

        private const double EDIT_REFRESH_DELAY = 0.3;
        private const double PLAY_REBUILD_INTERVAL = 0.5;
        private const double PLAY_REPAINT_INTERVAL = 0.1;

        private AViewTreeSource _source;
        private double _pendingRefreshTime = -1;
        private bool _rebuildRequested;
        private readonly HashSet<GameObject> _changedObjects = new();
        private double _lastRebuildTime;
        private double _lastRepaintTime;
        private bool _hasProblems;
        private HashSet<Object> _selectionToRestore;
        private HashSet<Object> _collapsed;

        private static GlobalPersistentContext<float> MenuWidthContext => PersistentContext.Get("ViewTreeWindow", "MenuWidth", 500f);
        private static GlobalPersistentContext<bool> ProblemsOnlyContext => PersistentContext.Get("ViewTreeWindow", "ProblemsOnly", false);

        // Validation runs in edit mode only, so the filter too
        private static bool ProblemsOnly => ProblemsOnlyContext.Value && !Application.isPlaying;

        public override float MenuWidth
        {
            get => MenuWidthContext.Value;
            set => MenuWidthContext.Value = Mathf.Max(200, value);
        }

        [MainToolbarElement("EDive/View Tree", defaultDockPosition = MainToolbarDockPosition.Left, defaultDockIndex = 50)]
        public static MainToolbarElement CreateToolbarButton()
        {
            return MainToolbarUtility.CreateElement(() =>
            {
                var button = new EditorToolbarButton
                {
                    icon = FontAwesomeEditorIcons.SitemapSolid.Raw,
                    tooltip = "View Tree"
                };
                button.AddToClassList("unity-editor-toolbar-element");
                button.clicked += ViewTreeWindow.OpenWindow;
                return button;
            });
        }

        [MenuItem("Tools/View Tree %h", priority = 120)]
        public static void OpenWindow()
        {
            var window = GetWindow<ViewTreeWindow>();
            window.SelectDefaultSource();
        }

        protected override void Initialize()
        {
            base.Initialize();
            titleContent = new GUIContent("View Tree", FontAwesomeEditorIcons.SitemapSolid.Highlighted);
            SelectDefaultSource();
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        protected override void OnDestroy()
        {
            ObjectChangeEvents.changesPublished -= OnChangesPublished;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            _source?.Dispose();
            _source = null;
            base.OnDestroy();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree();
            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle.Clone()
                .SetOffset(56)
                .SetIndentAmount(10)
                .SetTrianglePadding(-30);
            tree.Selection.SupportsMultiSelect = true;

            if (_source == null)
                return tree;

            var usedNames = new HashSet<string>();
            foreach (var root in _source.Roots)
            {
                var path = root.Name;
                for (var i = 1; !usedNames.Add(path); i++)
                    path = $"{root.Name}#{i}";

                root.SetRootPath(path);
                root.RefreshPaths();
                root.ValidateRecursively();
            }

            foreach (var item in _source.EnumerateItems())
            {
                // Problem rows and their ancestors
                if (!ProblemsOnly || item.SelfInvalid || item.ChildInvalid)
                    tree.Add(item.Path, item);
            }

            foreach (var menuItem in tree.EnumerateTree())
            {
                var item = (ViewTreeItem) menuItem.Value;
                menuItem.Name = item.Name;
                menuItem.OnDrawItem = DrawMenuItem;
                menuItem.OnRightClick = OnRightClickMenuItem;
                menuItem.Toggled = _collapsed == null || !_collapsed.Contains(item.Target);
                if (_selectionToRestore != null && _selectionToRestore.Contains(item.Target))
                    menuItem.Select(true);
            }

            _selectionToRestore = null;
            _collapsed = null;
            _hasProblems = _source.HasProblems;
            return tree;
        }

        protected override void DrawMenu()
        {
            GUILayout.Space(1);
            GUILayout.BeginHorizontal();

            var label = _source == null ? GUIHelper.TempContent("Select Source") : GUIHelper.TempContent(_source.Name, _source.Icon);
            GenericSelector<AViewTreeSource>.DrawSelectorDropdown(null, label, rect =>
            {
                var selector = new GenericSelector<AViewTreeSource>(null, false, GetAvailableSources());
                selector.SelectionTree.Config.DrawSearchToolbar = false;
                selector.SetSelection(_source);
                selector.EnableSingleClickToSelect();
                selector.SelectionTree.EnumerateTree().AddIcons(s => ((AViewTreeSource) s.Value).Icon);
                selector.SelectionConfirmed += selection => SetSource(selection.FirstOrDefault());
                selector.ShowInPopup(rect);
                return selector;
            });

            if (SirenixEditorGUI.IconButton(GetToolbarRect(), FontAwesomeEditorIcons.WandMagicSparklesSolid, "Select current"))
                SelectDefaultSource();

            if (SirenixEditorGUI.IconButton(GetToolbarRect(), EditorIcons.Refresh, "Rebuild"))
                RebuildSource();

            GUIHelper.PushColor(ProblemsOnlyContext.Value ? ERROR_COLOR : Color.white);
            if (SirenixEditorGUI.IconButton(GetToolbarRect(), ProblemsOnlyContext.Value ? FontAwesomeEditorIcons.FilterSolid : FontAwesomeEditorIcons.FilterRegular, "Problems only"))
            {
                ProblemsOnlyContext.Value = !ProblemsOnlyContext.Value;
                RebuildMenu();
            }
            GUIHelper.PopColor();

            GUILayout.Space(8);
            GUIHelper.PushGUIEnabled(_hasProblems);
            GUIHelper.PushColor(FIX_COLOR);
            if (GUILayout.Button("Fix All", GUILayout.MaxWidth(100)))
            {
                _source?.FixAll();
                RebuildSource();
            }
            GUIHelper.PopColor();
            GUIHelper.PopGUIEnabled();
            GUILayout.EndHorizontal();

            SirenixEditorGUI.HorizontalLineSeparator();
            base.DrawMenu();
            if (!MenuTree.EnumerateTree().Any())
                GUILayout.Label(GUIHelper.TempContent(" No view nodes found", FontAwesomeEditorIcons.TriangleExclamationSolid.Active), SirenixGUIStyles.LabelCentered, GUILayout.Height(18));

            var bottomRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            SirenixEditorGUI.DrawBorders(bottomRect, 0, 0, 1, 1);

            EditorGUILayout.BeginHorizontal(SirenixGUIStyles.ToolbarBackground, GUILayout.Height(20));
            if (SirenixEditorGUI.IconButton(GetToolbarRect(), FontAwesomeEditorIcons.SquarePlusSolid, "Expand all"))
                MenuTree.EnumerateTree(m => m.Toggled = true);
            if (SirenixEditorGUI.IconButton(GetToolbarRect(), FontAwesomeEditorIcons.SquareMinusSolid, "Collapse all"))
                MenuTree.EnumerateTree(m => m.Toggled = false);
            EditorGUILayout.EndHorizontal();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change is PlayModeStateChange.EnteredPlayMode or PlayModeStateChange.EnteredEditMode)
                RebuildSource();
        }

        // Property edits only revalidate, everything else changes rows
        private void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (var i = 0; i < stream.length; i++)
            {
                switch (stream.GetEventType(i))
                {
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                        stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var data);
                        AddChangedObject(EditorUtility.EntityIdToObject(data.instanceId));
                        break;
                    case ObjectChangeKind.CreateAssetObject:
                    case ObjectChangeKind.DestroyAssetObject:
                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        break;
                    default:
                        _rebuildRequested = true;
                        break;
                }
            }
            ScheduleRefresh();
        }

        private void AddChangedObject(Object changed)
        {
            var gameObject = changed switch
            {
                GameObject go => go,
                Component component => component.gameObject,
                _ => null
            };
            if (gameObject != null)
                _changedObjects.Add(gameObject);
        }

        // Runtime spawning and attaching
        private void OnHierarchyChanged()
        {
            if (!Application.isPlaying)
                return;

            _rebuildRequested = true;
            ScheduleRefresh();
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (_source == null || !_source.IsValid)
                SelectDefaultSource();
        }

        private void OnUndoRedo()
        {
            _rebuildRequested = true;
            ScheduleRefresh();
        }

        // Edit mode waits for changes to settle, play mode refreshes at most every interval
        private void ScheduleRefresh()
        {
            var now = EditorApplication.timeSinceStartup;
            if (!Application.isPlaying)
                _pendingRefreshTime = now + EDIT_REFRESH_DELAY;
            else if (_pendingRefreshTime < 0)
                _pendingRefreshTime = System.Math.Max(now, _lastRebuildTime + PLAY_REBUILD_INTERVAL);
        }

        private void OnEditorUpdate()
        {
            // Scene reloaded or switched, prefab stage closed
            if (_source != null && !_source.IsValid)
                SelectDefaultSource();

            var now = EditorApplication.timeSinceStartup;
            if (_pendingRefreshTime >= 0 && now >= _pendingRefreshTime)
            {
                _pendingRefreshTime = -1;
                Refresh();
            }

            // Keep runtime state icons live
            if (Application.isPlaying && now - _lastRepaintTime >= PLAY_REPAINT_INTERVAL)
            {
                _lastRepaintTime = now;
                Repaint();
            }
        }

        // Rebuilds only when rows no longer match, otherwise just revalidates
        private void Refresh()
        {
            if (this == null || _source == null)
            {
                _rebuildRequested = false;
                _changedObjects.Clear();
                return;
            }

            var rebuild = _rebuildRequested || (_changedObjects.Count > 0 && !_source.IsCurrent(_changedObjects));
            _rebuildRequested = false;
            _changedObjects.Clear();

            if (rebuild)
            {
                RebuildSource();
                return;
            }

            foreach (var root in _source.Roots)
                root.ValidateRecursively();
            _hasProblems = _source.HasProblems;

            // Filtered rows follow the problems
            if (ProblemsOnly)
                RebuildMenu();
            else
                Repaint();
        }

        private void RebuildSource()
        {
            if (this == null || _source == null)
                return;

            _lastRebuildTime = EditorApplication.timeSinceStartup;
            _source.Rebuild();
            RebuildMenu();
        }

        // Keeps selection and collapsed rows across the rebuild
        private void RebuildMenu()
        {
            if (MenuTree != null)
            {
                _selectionToRestore = new HashSet<Object>(MenuTree.Selection.Select(m => ((ViewTreeItem) m.Value).Target));
                _collapsed = new HashSet<Object>(MenuTree.EnumerateTree().Where(m => !m.Toggled).Select(m => ((ViewTreeItem) m.Value).Target));
            }
            ForceMenuTreeRebuild();
        }

        private static Rect GetToolbarRect() => GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));

        private void DrawMenuItem(OdinMenuItem menuItem)
        {
            var item = (ViewTreeItem) menuItem.Value;
            if (item.GameObject == null)
                return;

            // Type suffix
            var suffixRect = menuItem.LabelRect.HorizontalPadding(menuItem.Style.DefaultLabelStyle.CalcWidth(menuItem.Name), 0);
            GUIHelper.PushColor(TYPE_COLOR);
            GUI.Label(suffixRect, $"({item.TypeName})", EditorStyles.miniLabel);
            GUIHelper.PopColor();

            // Ping
            var pingRect = menuItem.LabelRect.AlignLeft(20).SubX(34);
            if (SirenixEditorGUI.IconButton(pingRect.Padding(2), FontAwesomeEditorIcons.CrosshairsSimpleSolid, "Ping"))
            {
                EditorGUIUtility.PingObject(item.GameObject);
                Selection.activeObject = item.GameObject;
            }

            // Object icon
            var (objectIcon, objectTooltip) = ViewTreeEditorUtils.GetObjectIcon(item.GameObject);
            GUI.Label(menuItem.LabelRect.AlignLeft(20).SubX(16), GUIHelper.TempContent(objectIcon, objectTooltip));

            if (item.Node == null)
            {
                DrawProblemIcons(menuItem, item, menuItem.Rect.AlignRight(20).SubX(4).VerticalPadding(2, 3));
                return;
            }

            // Right panel
            var panelRect = menuItem.Rect.AlignRight(125);
            EditorGUI.DrawRect(panelRect, SirenixGUIStyles.EditorWindowBackgroundColor);
            EditorGUI.DrawRect(panelRect, SirenixGUIStyles.MenuBackgroundColor);
            SirenixEditorGUI.DrawBorders(panelRect, 1, 0, 0, 0);

            var modes = ViewTreeEditorUtils.ATTACH_MODES;
            var controlRect = menuItem.Rect.AlignRight(modes.Length * 24).SubX(4);
            for (var i = 0; i < modes.Length; i++)
                DrawAttachModeButton(menuItem, item, modes[i], controlRect.Split(i, modes.Length), i, modes.Length);

            var iconRect = controlRect.AlignLeft(20).SubX(22).VerticalPadding(2, 3);
            if (Application.isPlaying)
            {
                DrawStateIcon(iconRect, item.Node.VisibleInTree, FontAwesomeEditorIcons.EyeSolid, FontAwesomeEditorIcons.EyeSlashSolid, "Visible");
                DrawStateIcon(iconRect.SubX(20), item.Node.FocusedInTree, FontAwesomeEditorIcons.HandPointerSolid, FontAwesomeEditorIcons.HandPointerSolid, "Focused");
                return;
            }

            DrawProblemIcons(menuItem, item, iconRect);
        }

        private static void DrawProblemIcons(OdinMenuItem menuItem, ViewTreeItem item, Rect iconRect)
        {
            if (item.ChildInvalid)
            {
                GUIHelper.PushColor(ERROR_COLOR);
                if (SirenixEditorGUI.IconButton(iconRect.Padding(1), FontAwesomeEditorIcons.SquareCaretDownSolid, "Child has problems"))
                    menuItem.GetChildMenuItemsRecursive(false).FirstOrDefault(m => ((ViewTreeItem) m.Value).SelfInvalid)?.Select();
                GUIHelper.PopColor();
            }

            if (item.SelfInvalid)
            {
                GUIHelper.PushColor(ERROR_COLOR);
                GUI.Label(iconRect.SubX(20), GUIHelper.TempContent(FontAwesomeEditorIcons.OctagonExclamationSolid.Highlighted, "Has problems"));
                GUIHelper.PopColor();
            }
        }

        private static void DrawStateIcon(Rect rect, bool state, EditorIcon onIcon, EditorIcon offIcon, string label)
        {
            GUIHelper.PushColor(state ? Color.white : new Color(1f, 1f, 1f, 0.3f));
            GUI.Label(rect, GUIHelper.TempContent((state ? onIcon : offIcon).Highlighted, $"{label}: {state}"));
            GUIHelper.PopColor();
        }

        private void DrawAttachModeButton(OdinMenuItem menuItem, ViewTreeItem item, AttachMode mode, Rect rect, int index, int count)
        {
            var style = index == 0 ? SirenixGUIStyles.MiniButtonLeft : index == count - 1 ? SirenixGUIStyles.MiniButtonRight : SirenixGUIStyles.MiniButtonMid;
            var selected = item.Node.AttachMode == mode;

            var prevIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(14, 14));
            var prevColor = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = mode.GetColor();

            var icon = mode.GetIcon();
            if (GUI.Button(rect.VerticalPadding(2, 0), GUIHelper.TempContent(selected ? icon.Highlighted : icon.Active, mode.ToString()), style))
            {
                var targets = MenuTree.Selection.Contains(menuItem)
                    ? MenuTree.Selection.Select(m => (ViewTreeItem) m.Value)
                    : new[] { item };

                foreach (var target in targets)
                    ViewTreeItem.SetAttachMode(target.Node, mode);

                foreach (var root in _source.Roots)
                    root.ValidateRecursively();
            }

            GUI.backgroundColor = prevColor;
            EditorGUIUtility.SetIconSize(prevIconSize);
        }

        private void OnRightClickMenuItem(OdinMenuItem menuItem)
        {
            var item = (ViewTreeItem) menuItem.Value;
            var menu = new GenericMenu();

            if (item.GameObject != null && PrefabUtility.IsPartOfPrefabInstance(item.GameObject))
            {
                menu.AddItem(new GUIContent("Open Prefab"), false, () => OpenPrefab(item.GameObject, false));
                menu.AddItem(new GUIContent("Open Prefab And Analyze"), false, () => OpenPrefab(item.GameObject, true));
            }

            if (item.SelfInvalid)
                menu.AddItem(new GUIContent("Fix"), false, item.Fix);
            if (item.ChildInvalid)
                menu.AddItem(new GUIContent("Fix Recursive"), false, item.FixRecursively);

            if (item.Node is ViewGroup)
            {
                foreach (var mode in ViewTreeEditorUtils.ATTACH_MODES)
                    menu.AddItem(new GUIContent($"Set Children {mode}"), false, () => item.SetChildrenAttachMode(mode));
            }

            if (menu.GetItemCount() > 0)
                menu.ShowAsContext();
        }

        private void OpenPrefab(GameObject instance, bool analyze)
        {
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance);
            var stage = PrefabStageUtility.OpenPrefab(path, instance, PrefabStage.Mode.InContext);
            if (analyze && stage != null)
                SetSource(new PrefabStageViewTreeSource(stage));
        }

        private IEnumerable<AViewTreeSource> GetAvailableSources()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                yield return Reuse(new PrefabStageViewTreeSource(stage));

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    yield return Reuse(new SceneViewTreeSource(scene));
            }
        }

        private AViewTreeSource Reuse(AViewTreeSource source) => Equals(_source, source) ? _source : source;

        private void SelectDefaultSource() => SetSource(GetAvailableSources().FirstOrDefault());

        private void SetSource(AViewTreeSource source)
        {
            if (!ReferenceEquals(_source, source))
                _source?.Dispose();

            _source = source;
            _source?.Rebuild();
            ForceMenuTreeRebuild();
        }
    }
}
