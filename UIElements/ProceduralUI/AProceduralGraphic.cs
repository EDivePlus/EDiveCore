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

        [SerializeField]
        [HideInInspector]
        private float _FrameWidth = 5f;

        [PropertyOrder(13)]
        [ShowIf(nameof(ShowFrameSettings))]
        [ShowInInspector]
        public float FrameWidth
        {
            get => _FrameWidth;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_FrameWidth, value)) return; _FrameWidth = value; SetVerticesDirty(); }
        }

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

        // Geometry shared by every vertex, matching DecodeGeometry in ProceduralShape.cginc:
        //   uv0.xy: grid code (added per vertex to x), width, height
        //   uv0.zw: roundness top right, bottom right, top left
        //   uv1.xy: roundness bottom left, arc apex x, arc apex y
        //   uv1.zw: arc start angle, arc sweep, arc corner radius
        protected static void PackGeometry(float width, float height, Vector4 roundness, Vector4 arc, float cornerRadius, out Vector4 uv0, out Vector4 uv1)
        {
            var size = VertexPacking.PackTriple(0f, VertexPacking.FixedPixel(width), VertexPacking.FixedPixel(height));
            var corners = VertexPacking.PackTriple(VertexPacking.FixedPixel(roundness.x), VertexPacking.FixedPixel(roundness.y), VertexPacking.FixedPixel(roundness.z));
            var apex = VertexPacking.PackTriple(VertexPacking.FixedPixel(roundness.w), VertexPacking.FixedSignedPixel(arc.x), VertexPacking.FixedSignedPixel(arc.y));
            var angles = VertexPacking.PackArcAngles(arc.z, arc.w);
            var arcData = VertexPacking.PackTriple(angles.x, angles.y, VertexPacking.FixedPixel(cornerRadius));
            uv0 = new Vector4(size.x, size.y, corners.x, corners.y);
            uv1 = new Vector4(apex.x, apex.y, arcData.x, arcData.y);
        }

        protected static float ResolveFrameOutwardExtension(bool noFill, float frameWidth, EdgePlacement placement)
        {
            if (!noFill)
                return 0f;

            var width = Mathf.Round(frameWidth);
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

    // Ranges the vertex encodings can hold; property setters clamp to them before the value is stored.
    // Everything travels in the uv channels and tangent.w, the only vertex data the canvas leaves untransformed.
    // Layouts match the decode functions in ProceduralShape.cginc.
    public static class VertexPacking
    {
        public const float MAX_PIXEL = 4095f;
        public const float MAX_OFFSET = 512f;
        public const float MAX_SHADOW_POWER = 255.99f;
        public const float MAX_RADIAL_SIZE = 40.95f;

        private const float FIXED_MAX = 65535f;
        private const float FIXED_STEP = 16f;
        private const float OFFSET_STEP = 4f;
        private const float FULL_TURN = Mathf.PI * 2f;

        public static float ClampPixel(float value) => Mathf.Clamp(value, 0f, MAX_PIXEL);
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

        // 16-bit fixed point in 1/16 px steps, up to 4095.9375 px
        public static float FixedPixel(float value) => Mathf.Clamp(Mathf.Round(value * FIXED_STEP), 0f, FIXED_MAX);

        // 16-bit fixed point, ±2048 px in 1/16 px steps
        public static float FixedSignedPixel(float value) => Mathf.Clamp(Mathf.Round((value + 2048f) * FIXED_STEP), 0f, FIXED_MAX);

        // 16-bit turn fraction, 65535 is a full turn
        public static float FixedAngle(float radians) => Mathf.Clamp(Mathf.Round(radians / FULL_TURN * FIXED_MAX), 0f, FIXED_MAX);

        // Three 16-bit values in two floats: a with the low byte of b, then the high byte of b with c
        public static Vector2 PackTriple(float a, float b, float c)
        {
            var bLow = b % 256f;
            var bHigh = (b - bLow) / 256f;
            return new Vector2(a + bLow * 65536f, bHigh + c * 256f);
        }

        // Grid vertex index and subdivision count, 5 bits each
        public static float PackGrid(int x, int y, int n) => x + y * 32f + n * 1024f;

        // Start angle wrapped to one turn and the sweep it covers
        public static Vector2 PackArcAngles(float start, float end)
        {
            var sweep = Mathf.Clamp(end - start, 0f, FULL_TURN);
            return new Vector2(FixedAngle(Mathf.Repeat(start, FULL_TURN)), FixedAngle(sweep));
        }

        // Shadow offset, 12 bits per axis: ±512 px in 1/4 px steps
        public static float PackOffsets(Vector2 offset) => PackOffset(offset.x) + PackOffset(offset.y) * 4096f;

        public static float QuantizeOffset(float value) => PackOffset(value) / OFFSET_STEP - MAX_OFFSET;

        private static float PackOffset(float value) => Mathf.Clamp(Mathf.Round((value + MAX_OFFSET) * OFFSET_STEP), 0f, 4095f);
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
