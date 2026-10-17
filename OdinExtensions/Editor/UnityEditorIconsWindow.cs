// Author: František Holubec
// Created: 16.10.2026

#if EDITOR_COROUTINES
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor
{
    public class UnityEditorIconsWindow : OdinEditorWindow
    {
        private const double MAX_DURATION_PER_FRAME_IN_MS = 10;
        private const string ICONS_FOLDER = "icons/";
        private const string DARK_PREFIX = "d_";

        [BoxGroup("Selected", false)]
        [HorizontalGroup("Selected/Row", 64)]
        [ShowInInspector]
        [HideLabel]
        [CustomValueDrawer(nameof(DrawPreview))]
        private Texture2D SelectedTexture => _selected?.Texture;

        [VerticalGroup("Selected/Row/Info")]
        [ShowInInspector]
        [EnableGUI]
        [DisplayAsString]
        [LabelWidth(50)]
        [InlineButton(nameof(CopyName), "Copy")]
        private string Name => _selected?.Name;

        [VerticalGroup("Selected/Row/Info")]
        [ShowInInspector]
        [EnableGUI]
        [DisplayAsString]
        [LabelWidth(50)]
        [InlineButton(nameof(CopyFileID), "Copy")]
        private string FileID => _selected?.FileID.ToString();

        [BoxGroup("Scripts")]
        [SerializeField]
        [HideLabel]
        [AssetsOnly]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<MonoScript> _Scripts = new();

        [PropertyOrder(10)]
        [HorizontalGroup("Filter")]
        [SerializeField]
        [HideLabel]
        [SuffixLabel("Search", true)]
        [OnValueChanged(nameof(RequestFilter))]
        private string _SearchFilter;

        [PropertyOrder(10)]
        [HorizontalGroup("Filter", 80)]
        [SerializeField]
        [ToggleLeft]
        [LabelText("Hide @2x")]
        [OnValueChanged(nameof(RequestFilter))]
        private bool _HideRetina = true;

        [PropertyOrder(10)]
        [HorizontalGroup("Filter", 110)]
        [SerializeField]
        [ToggleLeft]
        [LabelText("Hide other skin")]
        [OnValueChanged(nameof(RequestFilter))]
        private bool _HideOtherSkin = true;

        [PropertyOrder(10)]
        [SerializeField]
        [PropertyRange(20, 64)]
        private float _Size = 32;

        [PropertyOrder(15)]
        [ShowInInspector]
        [ShowIf(nameof(IsLoading))]
        [HideLabel]
        [ReadOnly]
        [ProgressBar(0, "$_totalCount")]
        private int LoadedCount => _icons.Count;

        private readonly EditorIconGrid _grid = new();
        private readonly List<IconData> _icons = new();
        private int _totalCount;
        private readonly List<IconData> _filtered = new();
        private HashSet<string> _iconNames;
        private IconData _selected;
        private EditorCoroutine _loadRoutine;
        private bool _filterRequested = true;

        [MenuItem("Tools/Unity Editor Icons")]
        public static void OpenWindow()
        {
            var window = GetWindow<UnityEditorIconsWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(600, 800);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            titleContent = new GUIContent("Unity Editor Icons", EditorGUIUtility.IconContent("d_Texture Icon").image);
            if (_icons.Count == 0 && _loadRoutine == null)
                _loadRoutine = EditorCoroutineUtility.StartCoroutine(LoadRoutine(), this);
            OnSelectionChange();
        }

        private void OnSelectionChange()
        {
            var scripts = Selection.objects.OfType<MonoScript>().ToList();
            if (scripts.Count == 0) return;
            _Scripts = scripts;
            Repaint();
        }

        private IEnumerator LoadRoutine()
        {
            var getBundle = typeof(EditorGUIUtility).GetMethod("GetEditorAssetBundle", BindingFlags.NonPublic | BindingFlags.Static);
            var bundle = getBundle?.Invoke(null, null) as AssetBundle;
            if (bundle == null)
            {
                UnityEngine.Debug.LogError("Editor asset bundle not found");
                yield break;
            }

            var assetNames = bundle.GetAllAssetNames().Where(n => n.StartsWith(ICONS_FOLDER)).ToList();
            _totalCount = assetNames.Count;

            var sw = Stopwatch.StartNew();
            foreach (var assetName in assetNames)
            {
                var texture = bundle.LoadAsset<Texture2D>(assetName);
                if (texture != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture, out _, out long fileID))
                    _icons.Add(new IconData(texture, fileID));

                if (sw.Elapsed.TotalMilliseconds > MAX_DURATION_PER_FRAME_IN_MS)
                {
                    RequestFilter();
                    yield return null;
                    sw.Restart();
                }
            }

            _icons.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            _loadRoutine = null;
            RequestFilter();
        }

        private void RequestFilter()
        {
            _filterRequested = true;
            Repaint();
        }

        private void RunFilter()
        {
            _iconNames = _icons.Select(i => i.Name).ToHashSet();
            var isProSkin = EditorGUIUtility.isProSkin;

            _filtered.Clear();
            foreach (var icon in _icons)
            {
                if (_HideRetina && icon.Name.Contains('@')) continue;
                if (_HideOtherSkin && IsOtherSkinVariant(icon.Name, isProSkin)) continue;
                if (!string.IsNullOrEmpty(_SearchFilter) && !FuzzySearch.Contains(_SearchFilter, icon.Name)) continue;
                _filtered.Add(icon);
            }

            _grid.ResetScroll();
            _filterRequested = false;
        }

        private bool IsOtherSkinVariant(string iconName, bool isProSkin)
        {
            var isDark = iconName.StartsWith(DARK_PREFIX);
            return isProSkin
                ? !isDark && _iconNames.Contains(DARK_PREFIX + iconName)
                : isDark && _iconNames.Contains(iconName.Substring(DARK_PREFIX.Length));
        }

        [PropertyOrder(20)]
        [OnInspectorGUI]
        private void DrawGrid()
        {
            if (_filterRequested && Event.current.type == EventType.Layout)
                RunFilter();

            var clicked = _grid.Draw(_filtered, i => i.Texture, i => i.Name, _Size, _filtered.IndexOf(_selected));
            if (clicked >= 0)
            {
                _selected = _filtered[clicked];
                GUIHelper.RemoveFocusControl();
            }
        }

        private bool IsLoading => _loadRoutine != null;
        private bool CanApply => _selected != null && _Scripts.Any(s => s != null);

        [BoxGroup("Scripts")]
        [HorizontalGroup("Scripts/Buttons")]
        [Button]
        [EnableIf(nameof(CanApply))]
        private void Apply() => SetIcon(_selected.Texture);

        [BoxGroup("Scripts")]
        [HorizontalGroup("Scripts/Buttons")]
        [Button("Reset")]
        private void ResetIcon() => SetIcon(null);

        private void SetIcon(Texture2D icon)
        {
            var paths = _Scripts
                .Where(s => s != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(p => p.StartsWith("Assets/"))
                .ToList();

            EditorApplication.delayCall += () =>
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var path in paths)
                    {
                        if (AssetImporter.GetAtPath(path) is not MonoImporter importer) continue;
                        importer.SetIcon(icon);
                        importer.SaveAndReimport();
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            };
        }

        private static Texture2D DrawPreview(Texture2D value)
        {
            var rect = GUILayoutUtility.GetRect(64, 64, GUILayoutOptions.Width(64).Height(64));
            SirenixEditorGUI.DrawSolidRect(rect, new Color(0, 0, 0, 0.2f));
            if (value != null)
                GUI.DrawTexture(rect.Padding(4), value, ScaleMode.ScaleToFit);
            return value;
        }

        private void CopyName() => Clipboard.Copy(_selected?.Name);
        private void CopyFileID() => Clipboard.Copy(FileID);

        private class IconData
        {
            public Texture2D Texture { get; }
            public string Name => Texture.name;
            public long FileID { get; }

            public IconData(Texture2D texture, long fileID)
            {
                Texture = texture;
                FileID = fileID;
            }
        }
    }
}
#endif
