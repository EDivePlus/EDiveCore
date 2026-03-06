#if UNITY_6000_3_OR_NEWER
#define UNITY_6_TOOLBAR
#endif

#if UNITY_EDITOR
using System.IO;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_6_TOOLBAR
using UnityEditor.Toolbars;
using UnityEngine.UIElements;
#else
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using EDIVE.External.ToolbarExtensions;
#endif

namespace EDIVE.AppLoading.Utils
{
    public static class LoaderEditorUtility
    {
        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
#if !UNITY_6_TOOLBAR
            ToolbarExtender.AddToLeftToolbar(PlayRootSceneToolbarGUI, 1000);
#endif
        }

#if UNITY_6_TOOLBAR
        [MainToolbarElement("EDive/Play Root Scene", defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = -1)]
        public static MainToolbarElement CreateToolbarButton()
        {
            return MainToolbarElementFactory.Create(() =>
            {
                var button = new Button(() => TryPlayRootScene());
                button.style.paddingLeft = 4;
                button.style.paddingRight = 4;
                button.style.flexDirection = FlexDirection.Row;
                button.style.alignItems = Align.Center;
                button.style.justifyContent = Justify.Center;
                button.AddToClassList("unity-toolbar-toggle");
                button.tooltip = "Play root scene";

                var bgCol = new Color(0.31f, 0.31f, 0.31f);
                var hoverCol = new Color(0.5f, 0.5f, 0.5f);

                button.style.backgroundColor = bgCol;
                button.RegisterCallback<MouseEnterEvent>(_ => 
                {
                    if (button.enabledInHierarchy) button.style.backgroundColor = hoverCol;
                });
                button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = bgCol);
                
                var icon = new Image
                {
                    image = FontAwesomeEditorIcons.RocketSolid.Raw,
                    style = { width = 14, height = 14 }
                };
                
                button.Add(icon);
                button.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                EditorApplication.playModeStateChanged += _ =>
                {
                    var enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
                    button.SetEnabled(enabled);
                    if (!enabled) button.style.backgroundColor = bgCol;
                };
                return button;
            });
        }
        
#else
        private static void PlayRootSceneToolbarGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            if (GUILayout.Button(GUIHelper.TempContent(FontAwesomeEditorIcons.RocketSolid.Highlighted, "Play Root scene"), ToolbarStyles.ToolbarButton, GUILayout.Width(30)))
            {
                EditorHelper.ExecuteNextFrame(() => TryPlayRootScene());
            }
            EditorGUI.EndDisabledGroup();
        }
#endif
        
        private static void SelectRootScene()
        {
            var path = EditorUtility.OpenFilePanel("Select Root Scene", Application.dataPath, "unity").Replace(Application.dataPath, "Assets");
            if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            {
                LoaderSettings.RootScene = path;
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange playModeStateChange)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!string.IsNullOrEmpty(PreviousScene))
            {
                EditorApplication.delayCall += ReloadPreviousSceneOnDelayCall;
            }
        }

        public static bool TryPlayRootScene()
        {
            if (string.IsNullOrEmpty(LoaderSettings.RootScene))
            {
                SelectRootScene();
                return false;
            }

            PreviousScene = SceneManager.GetActiveScene().path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            if (EditorSceneManager.OpenScene(LoaderSettings.RootScene).IsValid())
            {
                EditorApplication.isPlaying = true;
                return true;
            }

            if (EditorUtility.DisplayDialog("Root scene not found", $"Scene not found:\n{LoaderSettings.RootScene}\n\nWould you like to choose a different root scene?", "Yes", "No"))
            {
                SelectRootScene();
            }
            return false;
        }

        private static void ReloadPreviousSceneOnDelayCall()
        {
            EditorApplication.delayCall -= ReloadPreviousSceneOnDelayCall;

            if (!File.Exists(PreviousScene))
            {
                PreviousScene = null;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene(PreviousScene);
                PreviousScene = null;
            }
            else
            {
                EditorApplication.update += ReloadPreviousSceneOncePossible;
            }
        }

        private static void ReloadPreviousSceneOncePossible()
        {
            if (EditorApplication.isPlaying) return;

            if (File.Exists(PreviousScene))
                EditorSceneManager.OpenScene(PreviousScene);
            EditorApplication.update -= ReloadPreviousSceneOncePossible;
            PreviousScene = null;
        }

        private const string EDITOR_PREF_PREVIOUS_SCENE = "AppLoader.PreviousScene";
        private static string PreviousScene
        {
            get => EditorPrefs.GetString(PROJECT_PREFIX + EDITOR_PREF_PREVIOUS_SCENE, SceneManager.GetActiveScene().path);
            set => EditorPrefs.SetString(PROJECT_PREFIX + EDITOR_PREF_PREVIOUS_SCENE, value);
        }

        private static readonly string PROJECT_PREFIX = $"Proj{Animator.StringToHash(Application.dataPath)}_";
    }
}
#endif
