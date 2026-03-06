#if UNITY_EDITOR
using System.Linq;
using Meta.XR.Editor.UserInterface;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.External.Oculus
{
#if UNITY_TEXTMESHPRO
    [CustomEditor(typeof(TMPro.TextMeshPro))]
    [CanEditMultipleObjects]
    public class OVRTextFixEditor : TMPro.EditorUtilities.TMP_EditorPanel
    {
        public override void OnInspectorGUI()
        {
            var upgradeableCanvases = targets.OfType<TMPro.TextMeshPro>().ToArray();
            OVRCanvasFixEditor.UpgradeDialog("text", upgradeableCanvases, c =>
            {
                OVRCanvasFixEditor.SetTextDefaults(c);
                Undo.AddComponent<OVROverlayCanvas_TMPChanged>(c.gameObject).TargetCanvas = c;
            }, null, nameof(TMPro.TextMeshPro));
            base.OnInspectorGUI();
        }
    }
#endif

    [CustomEditor(typeof(Canvas))]
    [CanEditMultipleObjects]
    public class OVRCanvasFixEditor : OdinEditor
    {
        private int _presetSelection = 0;
        private GUIStyle _presetAreaStyle;
        private Editor _unityEditor;

        protected override void OnEnable()
        {
            base.OnEnable();
            _presetAreaStyle = new GUIStyle()
            {
                normal =
                {
                    background = Styles.Colors.DarkGray.ToTexture(),
                }
            };
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_unityEditor != null)
                DestroyImmediate(_unityEditor);
        }

        public override void OnInspectorGUI()
        {
            var upgradeableCanvases = targets.OfType<Canvas>().Where(c => GetRenderMode(c) == RenderMode.WorldSpace).ToArray();
            UpgradeDialog("canvas", upgradeableCanvases, c =>
                {
                    if (_presetSelection == 1)
                    {
                        SetTextDefaults(c);
                    }
                }, () =>
                {
                    using var verticalScope = new EditorGUILayout.VerticalScope(_presetAreaStyle, GUILayout.Width(120));
                    GUILayout.Label("Preset", Styles.GUIStyles.BoldLabel);
                    _presetSelection = GUILayout.SelectionGrid(_presetSelection, new[] {" Animated UI", " Static Text"}, 1, EditorStyles.radioButton);
                },
                $"{nameof(Canvas)}/{(_presetSelection == 0 ? "UI" : "Text")}");

            if (_unityEditor == null)
                _unityEditor = CreateEditor(targets, typeof(Editor).Assembly.GetType("UnityEditor.CanvasEditor"));

            if (_unityEditor != null)
                _unityEditor.OnInspectorGUI();
        }

        private static RenderMode GetRenderMode(Canvas canvas)
        {
            // canvas.renderMode returns ScreenSpaceOverlay when editing as a prefab,
            // even when it's actually set to WorldSpace.
            var serializedObject = new SerializedObject(canvas);
            serializedObject.Update();
            return (RenderMode) serializedObject.FindProperty("m_RenderMode").intValue;
        }

        internal static void UpgradeDialog(string noun, Component[] components, System.Action<OVROverlayCanvas> onUpgrade, System.Action onPresetArea, string telemetryParam)
        {
            if (components.Length == 0)
                return;

            if (!components.All(c => c.GetComponent<OVROverlayCanvas>() != null))
            {
                using (var verticalScope = new EditorGUILayout.VerticalScope(Styles.GUIStyles.DialogBox))
                {
                    using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                    {
#if META_SDK_85_OR_NEWER
                        var canvasLayerSelected = OVROverlayEditorHelper.HiddenCanvasLayerSelected;
#else
                       var canvasLayerSelected = OVROverlayEditorHelper.CanvasLayerSelected;
#endif
                        using (var disabledScope = new EditorGUI.DisabledGroupScope(!canvasLayerSelected))
                        {
                            if (GUILayout.Button(
                                    new GUIContent($" Upgrade {(components.Length == 1 ? "" : "all ")}to OVROverlayCanvas", Styles.Contents.MetaWhiteIcon.Image, ""),
                                    GUILayout.MaxHeight(40),
                                    GUILayout.ExpandWidth(true),
                                    GUILayout.ExpandHeight(true)))
                            {
                                foreach (var canvas in components)
                                {
                                    if (canvas.GetComponent<OVROverlayCanvas>() != null)
                                        continue;
                                    var overlay = Undo.AddComponent<OVROverlayCanvas>(canvas.gameObject);
#if META_SDK_85_OR_NEWER
                                    var overlayCanvasLayer = OVROverlayEditorHelper.HiddenCanvasLayer;
#else
                                    var overlayCanvasLayer = OVROverlayEditorHelper.CanvasLayer;
#endif
                                    overlay.SetCanvasLayer(overlayCanvasLayer, false);
                                    onUpgrade?.Invoke(overlay);
                                    EditorUtility.SetDirty(overlay);
                                    Debug.Log($"Added {nameof(OVROverlayCanvas)} to {canvas.gameObject}", overlay);
                                    OVRPlugin.SendEvent("canvas_upgrade_clicked", telemetryParam);
                                }
                            }

                            onPresetArea?.Invoke();
                        }

                        if (GUILayout.Button(Styles.Contents.InfoIcon, Styles.GUIStyles.MiniButton))
                        {
                            Application.OpenURL("https://developers.meta.com/horizon/documentation/unity/unity-ovroverlay/");
                        }
                    }

                    GUILayout.Label("Using OVROverlayCanvas will improve the visual clarity of this UI.", Styles.GUIStyles.DialogTextStyle);
                    GUILayout.Label("It will also improve the readability of any text.", Styles.GUIStyles.DialogTextStyle);
                    GUILayout.FlexibleSpace();

#if META_SDK_85_OR_NEWER
                    var canvasLayer = OVROverlayEditorHelper.HiddenCanvasLayer;
#else
                    var canvasLayer = OVROverlayEditorHelper.CanvasLayer;
#endif
                    OVROverlayEditorHelper.CanvasLayerSelectionUI(canvasLayer, _ => { }, _ => { });
                }
            }
            else
            {
                OVROverlayEditorHelper.DisplayMessage(OVROverlayEditorHelper.DisplayMessageType.Check, $"This {noun} is rendered using OVROverlayCanvas.");
            }
        }

        internal static void SetTextDefaults(OVROverlayCanvas c)
        {
#if META_SDK_85_OR_NEWER
            c.compositionMode = OVROverlayCanvas.CompositionMode.DepthTested;
            c._mipmapMode = OVROverlayCanvas.MipMapMode.Autogenerated;
#else
            c.overlayType = OVROverlay.OverlayType.Overlay;
            c._enableMipmapping = true;
#endif
            c.opacity = OVROverlayCanvas.DrawMode.OpaqueWithClip;
            c.manualRedraw = true;
            c._dynamicResolution = false;
        }
    }
}
#endif
