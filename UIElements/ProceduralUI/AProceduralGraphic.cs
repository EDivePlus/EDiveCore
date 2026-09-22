// Author: Michal Petr
// Created: 22.09.2026

using System.Collections.Generic;
using EDIVE.DataStructures;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using EDIVE.OdinExtensions.Editor;
#endif

namespace EDIVE.UIElements.ProceduralUI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class AProceduralGraphic : MaskableGraphic, IShapeStyleProvider
    {
        public const AdditionalCanvasShaderChannels REQUIRED_CANVAS_CHANNELS =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.TexCoord2 |
            AdditionalCanvasShaderChannels.TexCoord3 |
            AdditionalCanvasShaderChannels.Normal |
            AdditionalCanvasShaderChannels.Tangent;

        private static readonly Dictionary<Shader, Material> SHARED_MATERIALS = new();

        [SerializeField]
        [HideInInspector]
        private Shader _Shader;

        [PropertySpace]
        [PropertyOrder(11)]
        [ShowIf(nameof(HasOwnShape))]
        [IconEnumToggleButtons]
        [SerializeField]
        private FillMode _FillMode;

        [PropertyOrder(12)]
        [ShowIf(nameof(HasOwnShape))]
        [IconEnumToggleButtons]
        [SerializeField]
        private ShapeStyle _ShapeStyle;

        [PropertyOrder(13)]
        [ShowIf(nameof(ShowFrameSettings))]
        [MinValue(0f)]
        [SerializeField]
        private float _FrameWidth = 5f;

        [PropertyOrder(14)]
        [ShowIf(nameof(ShowFrameSettings))]
        [SerializeField]
        private EdgePlacement _FramePlacement;

#if UNITY_EDITOR
        private bool _editorRebuildQueued;
#endif

        [PropertySpace]
        [PropertyOrder(100)]
        [ShowInInspector]
        public bool RaycastTarget
        {
            get => raycastTarget;
            set => raycastTarget = value;
        }

        [PropertyOrder(101)]
        [ShowInInspector]
        public RectPadding RaycastPadding
        {
            get => raycastPadding;
            set => raycastPadding = value;
        }

        [PropertyOrder(102)]
        [ShowInInspector]
        public bool Maskable
        {
            get => maskable;
            set => maskable = value;
        }

        public FillMode FillMode
        {
            get => _FillMode;
            set { if (_FillMode == value) return; _FillMode = value; SetVerticesDirty(); }
        }

        public bool NoFill => _FillMode == FillMode.NoFill;

        public ShapeStyle ShapeStyle
        {
            get => _ShapeStyle;
            set { if (_ShapeStyle == value) return; _ShapeStyle = value; SetVerticesDirty(); }
        }

        public float FrameWidth
        {
            get => _FrameWidth;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_FrameWidth, value)) return; _FrameWidth = value; SetVerticesDirty(); }
        }

        public EdgePlacement FramePlacement
        {
            get => _FramePlacement;
            set { if (_FramePlacement == value) return; _FramePlacement = value; SetVerticesDirty(); }
        }

        protected abstract string ShaderName { get; }
        protected virtual bool HasOwnShape => true;
        private bool ShowFrameSettings => HasOwnShape && NoFill;
        protected float FrameOutwardExtension => ResolveFrameOutwardExtension(NoFill, _FrameWidth, _FramePlacement);

        public override Material defaultMaterial
        {
            get
            {
                if (!_Shader)
                    return base.defaultMaterial;

                if (!SHARED_MATERIALS.TryGetValue(_Shader, out var sharedMaterial) || !sharedMaterial)
                {
                    sharedMaterial = new Material(_Shader) { hideFlags = HideFlags.HideAndDontSave };
                    SHARED_MATERIALS[_Shader] = sharedMaterial;
                }
                return sharedMaterial;
            }
        }

        internal void SetShader(Shader shader)
        {
            if (_Shader == shader)
                return;
            _Shader = shader;
            SetMaterialDirty();
        }

        protected override void OnEnable()
        {
            EnsureShader();
            base.OnEnable();
            Refresh();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            Refresh();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Refresh();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            Refresh();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            Refresh();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            EnsureShader();
            EnsureAdditionalCanvasChannels();
        }

        protected override void OnValidate()
        {
            EnsureShader();
            base.OnValidate();
            Refresh();
        }
#endif

        public void Refresh()
        {
            EnsureAdditionalCanvasChannels();
            SetAllDirty();
#if UNITY_EDITOR
            QueueEditorRebuild();
#endif
        }

        protected virtual Vector4 GetRoundness() => Vector4.zero;
        protected virtual ShapeStyle ResolvedShapeStyle => _ShapeStyle;
        protected virtual CornerJoin ResolvedCornerJoin => CornerJoin.Round;

        // Matches DecodeCornerShape in ProceduralShape.cginc
        protected float EncodedCornerShape => (int) ResolvedShapeStyle + (int) ResolvedCornerJoin * 2;

        protected static float ResolveFrameOutwardExtension(bool noFill, float frameWidth, EdgePlacement placement)
        {
            if (!noFill)
                return 0f;

            var width = VertexPacking.RoundPixel(frameWidth);
            return placement switch
            {
                EdgePlacement.Center => width * 0.5f,
                EdgePlacement.Outside => width,
                _ => 0f
            };
        }

        private void EnsureShader()
        {
            if (!_Shader)
                _Shader = Shader.Find(ShaderName);
        }

        // Nested canvases keep their own channel set, so every canvas up to the root needs them
        private void EnsureAdditionalCanvasChannels()
        {
            var current = canvas;
            while (current)
            {
                current.additionalShaderChannels |= REQUIRED_CANVAS_CHANNELS;
                var parent = current.transform.parent;
                current = parent ? parent.GetComponentInParent<Canvas>(true) : null;
            }
        }

#if UNITY_EDITOR
        private void QueueEditorRebuild()
        {
            if (Application.isPlaying || _editorRebuildQueued)
                return;

            _editorRebuildQueued = true;
            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.delayCall += () =>
            {
                _editorRebuildQueued = false;
                if (!this || !isActiveAndEnabled)
                    return;

                EnsureAdditionalCanvasChannels();
                SetAllDirty();
                Canvas.ForceUpdateCanvases();
                SceneView.RepaintAll();
            };
        }
#endif
    }

    public interface IShapeStyleProvider
    {
        ShapeStyle ShapeStyle { get; }
    }

    // Ranges the vertex encodings can hold; everything sent to the shader is clamped to them.
    // Byte and offset layouts match UnpackBytes and DecodeOffset in ProceduralShape.cginc.
    public static class VertexPacking
    {
        public const float MAX_PIXEL = 4095f;
        public const float MAX_OFFSET = 2048f;
        public const float MAX_SHADOW_POWER = 255.99f;
        public const float MAX_RADIAL_SIZE = 40.95f;

        public static float ClampPixel(float value) => Mathf.Clamp(value, 0f, MAX_PIXEL);
        public static float RoundPixel(float value) => Mathf.Round(ClampPixel(value));
        public static float ClampShadowPower(float value) => Mathf.Clamp(value, 0f, MAX_SHADOW_POWER);
        public static float ClampRadialSize(float value) => Mathf.Clamp(value, 0f, MAX_RADIAL_SIZE);

        public static float PackBytes(int b0, int b1, int b2) => b0 + b1 * 256f + b2 * 65536f;

        // Outline, shadow and gradient color in four floats, three bytes each
        public static Vector4 PackColors(Color outline, Color shadow, Color gradient)
        {
            Color32 o = outline;
            Color32 s = shadow;
            Color32 g = gradient;
            return new Vector4(
                PackBytes(o.r, o.g, o.b),
                PackBytes(o.a, s.r, s.g),
                PackBytes(s.b, s.a, g.r),
                PackBytes(g.g, g.b, g.a));
        }

        // x = rgb, y = a
        public static Vector2 PackColor(Color color)
        {
            Color32 c = color;
            return new Vector2(PackBytes(c.r, c.g, c.b), c.a);
        }

        // 16-bit fixed point, ±2048 px in 1/16 px steps
        public static float PackOffset(float value) => Mathf.Round((Mathf.Clamp(value, -MAX_OFFSET, MAX_OFFSET - 0.0625f) + MAX_OFFSET) * 16f);

        public static float QuantizeOffset(float value) => PackOffset(value) / 16f - MAX_OFFSET;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AProceduralGraphic), true)]
    [CanEditMultipleObjects]
    public class ProceduralGraphicEditor : NativeWrapperOdinEditor<MaskableGraphic, GraphicEditor>
    {
        protected override BaseEditorDrawMode BaseEditorDrawMode => BaseEditorDrawMode.Hidden;
    }

    // Prefab stages open with their own canvases, which need the extra channels before the graphics can draw
    [InitializeOnLoad]
    public static class ProceduralUIPrefabStageRefresher
    {
        private static GameObject _queuedRoot;
        private static int _queuedPasses;
        private static bool _refreshQueued;

        static ProceduralUIPrefabStageRefresher()
        {
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            EditorApplication.hierarchyChanged += QueueCurrentPrefabStageRefresh;
            Undo.undoRedoPerformed += QueueCurrentPrefabStageRefresh;
        }

        private static void OnPrefabStageOpened(PrefabStage stage)
        {
            QueueRefresh(stage?.prefabContentsRoot, 3);
        }

        private static void QueueCurrentPrefabStageRefresh()
        {
            QueueRefresh(PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot);
        }

        private static void QueueRefresh(GameObject root, int passes = 2)
        {
            if (!root)
                return;

            _queuedRoot = root;
            _queuedPasses = Mathf.Max(_queuedPasses, passes);

            if (_refreshQueued)
                return;

            _refreshQueued = true;
            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.delayCall += RunQueuedRefresh;
        }

        private static void RunQueuedRefresh()
        {
            _refreshQueued = false;

            var root = _queuedRoot;
            if (!root)
                root = PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot;

            if (root)
                Refresh(root);

            _queuedPasses--;
            if (_queuedPasses <= 0 || !root)
            {
                _queuedRoot = null;
                _queuedPasses = 0;
                return;
            }

            _refreshQueued = true;
            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.delayCall += RunQueuedRefresh;
        }

        private static void Refresh(GameObject root)
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.additionalShaderChannels |= AProceduralGraphic.REQUIRED_CANVAS_CHANNELS;
                if (canvas.rootCanvas)
                    canvas.rootCanvas.additionalShaderChannels |= AProceduralGraphic.REQUIRED_CANVAS_CHANNELS;
            }

            foreach (var glowModifier in root.GetComponentsInChildren<GlowModifier>(true))
                glowModifier.Refresh();

            foreach (var graphic in root.GetComponentsInChildren<AProceduralGraphic>(true))
                graphic.Refresh();

            Canvas.ForceUpdateCanvases();
            SceneView.RepaintAll();
        }
    }
#endif
}
