using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
using Path = System.IO.Path;

namespace EDIVE.OdinExtensions.Editor
{
    public class CustomEditorIconsWindow : OdinMenuEditorWindow
    {
        private static readonly CustomEditorIconsOverview ICONS_OVERVIEW = new();
        private static readonly ConvertPngToBase64 PNG_TO_BASE64 = new();

        [MenuItem("Tools/Editor Icons")]
        public static void OpenWindow()
        {
            var window = GetWindow<CustomEditorIconsWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(600, 800);
            window.SetupWindowStyle();
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            SetupWindowStyle();
        }
        
        protected override void Initialize()
        {
            base.Initialize();
            SetupWindowStyle();
        }
        
        private void SetupWindowStyle()
        {
            titleContent = new GUIContent("Editor Icons", EditorIcons.ImageCollection.Active);
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(true)
            {
                {"Icons Overview", ICONS_OVERVIEW, FontAwesomeEditorIcons.EyeSolid},
                {"PNG to Base64", PNG_TO_BASE64, FontAwesomeEditorIcons.ArrowRightArrowLeftSolid},
            };
            tree.DefaultMenuStyle = new OdinMenuStyle();
            return tree;
        }
    }

    [Serializable]
    public class CustomEditorIconsOverview
    {
        private const double MAX_DURATION_PER_FRAME_IN_MS = 10;
        
        [PropertyOrder(-10)]
        [ShowInInspector]
        [EnumToggleButtons]
        [HideLabel]
        private EditorIconsBundle Category
        {
            get => _category;
            set
            {
                var prevValue = _category;
                _category = value;
                if (prevValue != value)
                    RefreshIcons();
            }
        }
        private EditorIconsBundle _category = EditorIconsBundle.Odin;

        [PropertySpace(SpaceBefore = 0, SpaceAfter = 4)]
        [PropertyOrder(-10)]
        [ShowInInspector]
        private string StringReferencePrefix => Category.GetPrefix();

        [HorizontalGroup("Search")]
        [SerializeField]
        private string _SearchFilter;

        [SerializeField]
        [PropertyRange(20, 50)]
        private float _Size = 28;

        private List<Tuple<Texture, string>> _icons = new List<Tuple<Texture, string>>();

        private string _prevSearch = "123";
        private float _scrollPos;
        private float _scrollMax;

        [ShowIf(nameof(Category), EditorIconsBundle.FontAwesome)]
        [HorizontalGroup("Search", 120)]
        [Button("Online Overview")]
        private void OpenFontAwesomeWebsite()
        {
            Application.OpenURL("https://fontawesome.com/search?o=r&s=solid%2Cregular");
        }

        private Dictionary<EditorIconsBundle, List<Tuple<Texture, string>>> _cachedIcons;

        private bool _refreshRequested;

        private void RefreshIcons()
        {
            _cachedIcons ??= new Dictionary<EditorIconsBundle, List<Tuple<Texture, string>>>();
            if (!_cachedIcons.TryGetValue(Category, out var icons))
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(RefreshIconRoutine(_category));
            }
            _refreshRequested = true;
            _icons ??= new List<Tuple<Texture, string>>();
        }

        private IEnumerator RefreshIconRoutine(EditorIconsBundle category)
        {
            var icons = _cachedIcons[category] = new List<Tuple<Texture, string>>();
            icons.Clear();
            var iconProperties = GetTypeForCategory(category)
                .GetProperties(Flags.StaticPublic)
                .OrderBy(p => p.Name);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            
            foreach (var iconProperty in iconProperties)
            {
                var returnType = iconProperty.GetReturnType();
                Texture texture = null;
                if (typeof(Texture).IsAssignableFrom(returnType))
                {
                    texture = (Texture) iconProperty.GetGetMethod().Invoke(null, null);
                }
                else if (typeof(EditorIcon).IsAssignableFrom(returnType))
                {
                    texture = ((EditorIcon) iconProperty.GetGetMethod().Invoke(null, null)).Raw;
                }

                var iconTexture = new Tuple<Texture, string>(texture, iconProperty.Name);
                icons.Add(iconTexture);
                
                if (sw.Elapsed.TotalMilliseconds > MAX_DURATION_PER_FRAME_IN_MS)
                {
                    yield return null;
                    sw.Restart();
                }
                
                _refreshRequested = true;
            }
        }
        
        [OnInspectorGUI]
        private void OnInspectorGUI()
        {
            // Update
            if (Event.current.type == EventType.Layout)
            {
                if (_prevSearch != _SearchFilter || (_icons == null || _icons.Count == 0))
                {
                    RefreshIcons();
                    _prevSearch = _SearchFilter;
                }
            }
            
            if (!_cachedIcons.TryGetValue(Category, out var icons))
            {
                RefreshIcons();
            }
            else if (_refreshRequested)
            {
                _icons ??= new List<Tuple<Texture, string>>();
                _icons.Clear();
                foreach (var icon in icons)
                {
                    if (icon == null || icon.Item1 == null) continue;
                    if (!string.IsNullOrEmpty(_SearchFilter) && !FuzzySearch.Contains(_SearchFilter, icon.Item2))
                        continue;
                    
                    _icons.Add(icon);
                }
                _refreshRequested = false;
            }
            
            
            _scrollPos = EditorGUILayout.BeginScrollView(new Vector2(0, _scrollPos)).y;
            {
                var area = GUILayoutUtility.GetRect(0, _scrollMax, GUIStyle.none, GUILayoutOptions.ExpandHeight());
                EditorGUI.DrawRect(area, Color.clear);
                if (Event.current.type == EventType.Repaint)
                {
                    var yMax = 0f;
                    const int padding = 10;
                    var s = _Size + padding;
                    var num = (int) (area.width / s);
                    var remain = (area.width % s) / num;
                    s += remain;
                    area.width += 1;
                    var mp = Event.current.mousePosition;
                    var mouseOver = -1;

                    for (var i = 0; i < _icons.Count; i++)
                    {
                        var cell = area.SplitGrid(s, s, i);
                        yMax = Math.Max(yMax, cell.yMax);

                        if (cell.Contains(mp))
                        {
                            mouseOver = i;
                        }

                        cell = cell.AlignCenter(_Size, _Size);

                        GUI.DrawTexture(cell.Padding(5), _icons[i].Item1, ScaleMode.ScaleToFit);
                    }

                    _scrollMax = yMax;

                    if (mouseOver >= 0)
                    {
                        var style = new GUIStyle(SirenixGUIStyles.WhiteLabel)
                        {
                            fontStyle = FontStyle.Bold
                        };

                        var labelName = _icons[mouseOver].Item2;
                        var size = style.CalcSize(new GUIContent(labelName));
                        var pos = Event.current.mousePosition + new Vector2(30, 5);
                        var rect = new Rect(pos, size);

                        var push = rect.xMax - area.xMax + 20;
                        if (push > 0)
                        {
                            rect.x -= push;
                        }

                        EditorGUI.DrawRect(rect.Expand(10, 5), Color.black);
                        GUI.Label(rect, labelName, style);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            if (Event.current.type == EventType.MouseDown)
            {
                GUIHelper.RemoveFocusControl();
            }
        }

        private static Type GetTypeForCategory(EditorIconsBundle category)
        {
            return category switch
            {
                EditorIconsBundle.Odin => typeof(EditorIcons),
                EditorIconsBundle.Custom => typeof(CustomEditorIcons),
                EditorIconsBundle.FontAwesome => typeof(FontAwesomeEditorIcons),
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }
    }
    
    [Serializable]
    public class ConvertPngToBase64
    {
        [PropertyOrder(-10)]
        [Button]
        public void OpenFile()
        {
            var filePath = EditorUtility.OpenFilePanel("Select PNG", string.Empty, "png");
            if(string.IsNullOrWhiteSpace(filePath)) return;

            var bytes = File.ReadAllBytes(filePath);
            Base64Image = Convert.ToBase64String(bytes);
            PropertyName = Nicify(Path.GetFileNameWithoutExtension(filePath).Replace("-", " ")).Replace(" ", "");
        }
        
        public static string Nicify(string original)
        {
            var camel = Regex.Replace(original, "(\\B[A-Z])", " $1");
            var snake = camel.Replace("_", " ");
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(snake);
        }
        
        [ShowInInspector]
        [MultiLineProperty(4)]
        [LabelWidth(100)]
        private string Base64Image { get; set; }
        
        [PropertySpace]
        [ShowInInspector]
        private string PropertyName { get; set; }
        
        [PropertySpace]
        [ShowInInspector]
        private PropertyTypeType PropertyType { get; set; }

        private bool EnableCopy => !string.IsNullOrWhiteSpace(Base64Image) && !string.IsNullOrWhiteSpace(PropertyName);
        
        [Button]
        [EnableIf(nameof(EnableCopy))]
        public void CopyProperty()
        {
            var privateLabel = $"_{ToLowerFirstChar(PropertyName)}";
            var stringBuilder = new StringBuilder();
            switch (PropertyType)
            {
                case PropertyTypeType.EditorIcon:
                    stringBuilder.Append($"private static EditorIcon {privateLabel};\n");
                    stringBuilder.Append($"public static EditorIcon {PropertyName} => {privateLabel} ??= new LazyEditorIcon(34, 34,\n");
                    stringBuilder.Append($"\t\"{Base64Image}\");\n\n");
                    break;
                    
                case PropertyTypeType.Texture:
                    stringBuilder.Append($"private static Texture2D {privateLabel};\n");
                    stringBuilder.Append($"public static Texture2D {PropertyName} => {privateLabel} != null ? {privateLabel} : TextureUtilities.LoadImage(32, 32, Convert.FromBase64String(\n");
                    stringBuilder.Append($"\t\"{Base64Image}\"));\n\n");
                    break;
                
                default: throw new ArgumentOutOfRangeException();
            }
            Clipboard.Copy(stringBuilder.ToString());
        }

        private static string ToLowerFirstChar(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToLower(input[0]) + input.Substring(1);
        }

        private enum PropertyTypeType
        {
            EditorIcon,
            Texture
        }
    }


    
}
