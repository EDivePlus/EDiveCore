#if UNITY_EDITOR
using System.IO;
using EDIVE.EditorUtils;
using EDIVE.External.ToolbarExtensions;
using EDIVE.OdinExtensions;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.AppLoading.Utils
{
    public static class LoaderToolbarExtensions
    {
        private static Scene? _currentManagersScene;
        
        [InitializeOnLoadMethod]
        private static void InitializeToolbar()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            ToolbarExtender.AddToLeftPlayButtons(PlayRootSceneToolbarGUI, 0);
            ToolbarExtender.AddToLeftToolbar(ManagersSceneToolbarGUI, -90);
        }

        private static void ManagersSceneToolbarGUI()
        {
            GUILayout.Space(2);
            var dropdownRect = GUILayoutUtility.GetRect(0, 18).MinWidth(200);
            if (GUILayout.Button(new GUIContent(null, FontAwesomeEditorIcons.LoaderSolid.Highlighted, "Open Loader Settings"), ToolbarStyles.ToolbarButton, GUILayout.Width(30)))
            {
                LoaderSettings.OpenLoaderSettings();
            }

            if (GUILayout.Button(new GUIContent(null, FontAwesomeEditorIcons.CaretDownSolid.Active, "Loader Menu"), ToolbarStyles.ToolbarButton, GUILayout.Width(15)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Disable Parallel Load"), LoaderUtils.DisableParallelLoad, () => LoaderUtils.DisableParallelLoad = !LoaderUtils.DisableParallelLoad);
                menu.DropDown(dropdownRect);
            }
            GUILayout.Space(2);
        }

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

        public static void TryPlayRootScene()
        {
            if (string.IsNullOrEmpty(LoaderSettings.RootScene))
            {
                SelectRootScene();
                return;
            }

            PreviousScene = SceneManager.GetActiveScene().path;
            EditorHelper.ExecuteNextFrame(() =>
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                if (EditorSceneManager.OpenScene(LoaderSettings.RootScene).IsValid())
                {
                    EditorApplication.isPlaying = true;
                    return;
                }

                if (EditorUtility.DisplayDialog("Root scene not found", $"Scene not found:\n{LoaderSettings.RootScene}\n\nWould you like to choose a different root scene?", "Yes", "No"))
                {
                    SelectRootScene();
                }
            });
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

        private static void PlayRootSceneToolbarGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            if (GUILayout.Button(GUIHelper.TempContent(FontAwesomeEditorIcons.RocketSolid.Highlighted, "Play Root scene"), ToolbarStyles.ToolbarButtonBiggerIcon, GUILayout.Width(30)))
            {
                TryPlayRootScene();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(2);
        }
    }
}
#endif
