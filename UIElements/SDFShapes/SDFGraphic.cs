// Author: Michal Petr
// Created: 22.09.2026

using System.Collections.Generic;
using EDIVE.DataStructures;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using EDIVE.OdinExtensions.Editor;
#endif

namespace EDIVE.UIElements.SDFShapes
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class SDFGraphic : MaskableGraphic, ICanvasRaycastFilter, ICornerStyleProvider
    {
        private const string SHADER_NAME = "Hidden/EDIVE/SDFShapes/SDFShape";

        public const AdditionalCanvasShaderChannels REQUIRED_CANVAS_CHANNELS =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.TexCoord2;

        private static readonly int[] EFFECT_DRAW_ORDERS = { -1, 1, 2 };
        private const float MIN_HOLE_SIZE = 32f;
        private const float SQRT_HALF = 0.70710678f;
        private static Material _sharedMaterial;

        [SerializeField]
        [HideInInspector]
        private Shader _Shader;

        [SerializeField]
        private Texture _Texture;

        [ShowIf(nameof(HasTexture))]
        [SerializeField]
        private TextureFit _TextureFit;

        // Graphic.color is the fill color, or tints the gradient; its alpha also fades effects that use it
        [PropertyOrder(1)]
        [ShowInInspector]
        private Color Color
        {
            get => color;
            set => color = value;
        }

        [PropertyOrder(2)]
        [HideIf(nameof(HasTexture))]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private GradientFill _Fill = GradientFill.Default;

        [PropertyOrder(3)]
        [PropertySpace]
        [SerializeField]
        private CornerRoundness _Roundness;

        [PropertyOrder(3)]
        [IconEnumToggleButtons]
        [FormerlySerializedAs("_ShapeStyle")]
        [SerializeField]
        private CornerStyle _CornerStyle;

        [PropertyOrder(3)]
        [Tooltip("How frames, outlines, outer shadows and arc cuts wrap sharp corners")]
        [ShowIf("ShowCornerWrap")]
        [IconEnumToggleButtons]
        [FormerlySerializedAs("_CornerJoin")]
        [SerializeField]
        private SharpCornerWrap _SharpCornerWrap;

        [PropertyOrder(4)]
        [PropertySpace]
        [IconEnumToggleButtons]
        [SerializeField]
        private FillMode _FillMode;

        [SerializeField]
        [HideInInspector]
        private float _FrameWidth = 5f;

        [PropertyOrder(5)]
        [ShowIf(nameof(NoFill))]
        [ShowInInspector]
        public float FrameWidth
        {
            get => _FrameWidth;
            set { value = VertexPacking.ClampFrameWidth(value); if (Mathf.Approximately(_FrameWidth, value)) return; _FrameWidth = value; SetVerticesDirty(); }
        }

        [PropertyOrder(6)]
        [ShowIf(nameof(NoFill))]
        [SerializeField]
        private EdgePlacement _FramePlacement;

        private readonly List<ASDFEffect> _effects = new();
        private SDFArc _arc;
        private bool _componentsDirty = true;
        private ArcCutout _activeArc;
        private float _maxCornerRadius;
        [System.NonSerialized]
        private int _gradientRow = -1;

#if UNITY_EDITOR
        private bool _editorRebuildQueued;
#endif

        [PropertyOrder(7)]
        [PropertySpace]
        [ShowInInspector]
        public bool RaycastTarget
        {
            get => raycastTarget;
            set => raycastTarget = value;
        }

        [PropertyOrder(7)]
        [ShowInInspector]
        public RectPadding RaycastPadding
        {
            get => raycastPadding;
            set => raycastPadding = value;
        }

        [PropertyOrder(7)]
        [ShowInInspector]
        public bool Maskable
        {
            get => maskable;
            set => maskable = value;
        }

        public Texture Texture
        {
            get => _Texture;
            set { if (_Texture == value) return; _Texture = value; SetVerticesDirty(); SetMaterialDirty(); }
        }

        public TextureFit TextureFit
        {
            get => _TextureFit;
            set { if (_TextureFit == value) return; _TextureFit = value; SetVerticesDirty(); }
        }

        public GradientFill Fill
        {
            get => _Fill;
            set { _Fill = value; SetVerticesDirty(); }
        }

        public CornerRoundness Roundness
        {
            get => _Roundness;
            set { _Roundness = value; SetVerticesDirty(); }
        }

        public FillMode FillMode
        {
            get => _FillMode;
            set { if (_FillMode == value) return; _FillMode = value; SetVerticesDirty(); }
        }

        public bool NoFill => _FillMode == FillMode.NoFill;

        public CornerStyle CornerStyle
        {
            get => _CornerStyle;
            set { if (_CornerStyle == value) return; _CornerStyle = value; SetVerticesDirty(); }
        }

        public EdgePlacement FramePlacement
        {
            get => _FramePlacement;
            set { if (_FramePlacement == value) return; _FramePlacement = value; SetVerticesDirty(); }
        }

        public SharpCornerWrap SharpCornerWrap
        {
            get => _SharpCornerWrap;
            set { if (_SharpCornerWrap == value) return; _SharpCornerWrap = value; SetVerticesDirty(); }
        }

        public ArcCutout ActiveArc
        {
            get
            {
                RefreshComponents();
                return _arc && _arc.isActiveAndEnabled ? _arc.Arc : ArcCutout.Default;
            }
        }

        // Without a texture every graphic shares the gradient atlas, so they batch
        public override Texture mainTexture => _Texture ? _Texture : SDFGradientAtlas.Texture;

        public override Material defaultMaterial
        {
            get
            {
                if (!_Shader)
                    return base.defaultMaterial;

                if (!_sharedMaterial || _sharedMaterial.shader != _Shader)
                    _sharedMaterial = new Material(_Shader) { hideFlags = HideFlags.HideAndDontSave };
                return _sharedMaterial;
            }
        }

        // Matches DecodeCornerShape in SDFShape.cginc
        private float EncodedCornerShape => (int) _CornerStyle + (int) _SharpCornerWrap * 2;
        private float FrameWidthQuantized => VertexPacking.QuantizeFrameWidth(_FrameWidth);
        private float FrameReach => NoFill ? _FramePlacement.OuterExtent(FrameWidthQuantized) : 0f;
        private bool HasVisibleFill => color.a > 0f;
        private bool HasTexture => _Texture;
        private bool UsesGradient => !_Texture && _Fill.HasGradient;
        private bool HasOpaqueFill => !NoFill && !_Texture && color.a >= 1f && _Fill.IsOpaque;

        protected override void OnEnable()
        {
            EnsureShader();
            MigrateLegacyFill();
            base.OnEnable();
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SDFGradientAtlas.Release(ref _gradientRow);
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            Refresh();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
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
            MigrateLegacyFill();
            base.OnValidate();
            Refresh();
        }

        [PropertyOrder(7)]
        [PropertySpace]
        [OnInspectorGUI]
        private void DrawAddEffectMenu()
        {
            const float width = 100f;
            var rect = EditorGUILayout.GetControlRect();
            rect.xMin = rect.xMax - width;
            if (!EditorGUI.DropdownButton(rect, new GUIContent("Add Effect"), FocusType.Passive, EditorStyles.miniPullDown))
                return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Shadow"), false, () => Undo.AddComponent<SDFShadow>(gameObject));
            menu.AddItem(new GUIContent("Outline"), false, () => Undo.AddComponent<SDFOutline>(gameObject));
            if (TryGetComponent<SDFArc>(out _))
                menu.AddDisabledItem(new GUIContent("Arc"));
            else
                menu.AddItem(new GUIContent("Arc"), false, () => Undo.AddComponent<SDFArc>(gameObject));
            menu.DropDown(rect);
        }
#endif

        // Called by effects and arc when they are enabled or disabled
        internal void SetComponentsDirty()
        {
            _componentsDirty = true;
            SetVerticesDirty();
        }

        private void RefreshComponents()
        {
#if UNITY_EDITOR
            // Reordering components in the inspector sends no message
            if (!Application.isPlaying)
                _componentsDirty = true;
#endif
            if (!_componentsDirty)
                return;

            GetComponents(_effects);
            TryGetComponent(out _arc);
            _componentsDirty = false;
        }

        public void Refresh()
        {
            EnsureAdditionalCanvasChannels();
            SetAllDirty();
#if UNITY_EDITOR
            QueueEditorRebuild();
#endif
        }

        // Hits only inside the shape, grown by outward raycast padding. Frames count their empty middle too.
#if UNITY_EDITOR
        // Only sharp corners grown outward use it: a frame past the edge, an outward outline, an outer shadow, or an arc cut
        private bool ShowCornerWrap
        {
            get
            {
                var arc = ActiveArc;
                if (!arc.IsFull && arc.CornerRadius <= 0f)
                    return true;

                var rect = rectTransform.rect;
                var radii = _Roundness.Resolve(rect.width, rect.height);
                var hasSharpCorner = _CornerStyle == CornerStyle.Chamfer || Mathf.Min(Mathf.Min(radii.x, radii.y), Mathf.Min(radii.z, radii.w)) <= 0f;
                if (!hasSharpCorner)
                    return false;

                if (NoFill && _FramePlacement != EdgePlacement.Inside)
                    return true;

                foreach (var effect in GetComponents<ASDFEffect>())
                {
                    if (!effect.enabled)
                        continue;
                    if (effect is SDFOutline outline && outline.Placement != EdgePlacement.Inside)
                        return true;
                    if (effect is SDFShadow shadow && shadow.Mode == ShadowMode.Outer)
                        return true;
                }
                return false;
            }
        }
#endif

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local))
                return false;

            var rect = rectTransform.rect;
            var p = local - rect.center;
            var padding = raycastPadding;
            var grow = Mathf.Max(0f, -Mathf.Min(Mathf.Min(padding.x, padding.y), Mathf.Min(padding.z, padding.w)));
            if (ShapeDistance(p, rect.size * 0.5f, _Roundness.Resolve(rect.width, rect.height)) > grow)
                return false;

            var arc = ActiveArc;
            return arc.IsFull || arc.Contains(p, rect.width, rect.height);
        }

        // Mirrors ShapeDistance in SDFShape.cginc; p is in px from the rect center
        private float ShapeDistance(Vector2 p, Vector2 halfSize, Vector4 corners)
        {
            var radius = p.x > 0f ? (p.y > 0f ? corners.x : corners.y) : (p.y > 0f ? corners.z : corners.w);
            radius = Mathf.Min(radius, Mathf.Min(halfSize.x, halfSize.y));
            var q = new Vector2(Mathf.Abs(p.x) - halfSize.x, Mathf.Abs(p.y) - halfSize.y);
            if (_CornerStyle == CornerStyle.Chamfer && radius > 0f)
                return Mathf.Max(Mathf.Max(q.x, q.y), (q.x + q.y + radius) * SQRT_HALF);

            var qc = new Vector2(q.x + radius, q.y + radius);
            return Mathf.Min(Mathf.Max(qc.x, qc.y), 0f) + Vector2.Max(qc, Vector2.zero).magnitude - radius;
        }

        // Outer shadows, then the fill, inner shadows and outlines; component order within each group
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = rectTransform.rect;
            _activeArc = ActiveArc;
            if (rect.width <= 0f || rect.height <= 0f || _activeArc.IsEmpty)
                return;

            var context = BuildEffectContext();
            var geometry = BuildGeometryVertex(rect.width, rect.height);

            AddEffects(vh, geometry, context, EFFECT_DRAW_ORDERS[0]);

            SDFGradientAtlas.Assign(UsesGradient ? _Fill.Gradient : null, ref _gradientRow);
            AddFill(vh, geometry, rect, context.FillInset);

            for (var i = 1; i < EFFECT_DRAW_ORDERS.Length; i++)
                AddEffects(vh, geometry, context, EFFECT_DRAW_ORDERS[i]);
        }

        private SDFEffectContext BuildEffectContext()
        {
            var hasVisibleFill = HasVisibleFill;
            var outerReach = 0f;
            var innerReach = 0f;
            var fillInset = 0f;
            foreach (var effect in _effects)
            {
                if (effect is not SDFOutline outline || !outline || !outline.isActiveAndEnabled)
                    continue;

                outerReach = Mathf.Max(outerReach, outline.OuterReach);
                innerReach = Mathf.Max(innerReach, outline.InnerReach + outline.GetFillOverlap(hasVisibleFill));
                fillInset = Mathf.Max(fillInset, outline.FillInset);
            }
            return new SDFEffectContext(Mathf.Round(outerReach), Mathf.Round(innerReach), fillInset, hasVisibleFill, HasOpaqueFill);
        }

        private void AddEffects(VertexHelper vh, UIVertex geometry, in SDFEffectContext context, int drawOrder)
        {
            foreach (var effect in _effects)
            {
                if (!effect || !effect.isActiveAndEnabled || DrawOrder(effect.Layer) != drawOrder)
                    continue;

                var data = effect.PackData(context);
                var vertex = geometry;
                vertex.uv2 = new Vector4(EncodeLayer(effect.Layer), data.x, data.y, data.z);
                var effectColor = effect.LayerColor;
                if (effect.UseGraphicAlpha)
                    effectColor.a *= color.a;
                vertex.color = effectColor;
                var holeInset = NoFill ? FrameHoleInset(effect.GetFrameHoleInset(context)) : effect.GetHoleInset(context);
                AddQuad(vh, vertex, rectTransform.rect, FrameReach + effect.GetReach(context) + 1f, HoleInset(holeInset), out _);
            }
        }

        // Frames are empty past their inner edge, layers reach into that by the given amount
        private float FrameHoleInset(float reach) => reach < 0f ? -1f : FrameWidthQuantized - FrameReach + reach;

        // Hole past the corner radius; arcs cut into the middle, so only full shapes get one
        private float HoleInset(float inset) => _activeArc.IsFull && inset >= 0f ? _maxCornerRadius + inset : -1f;

        private static int DrawOrder(SDFLayer layer) => layer switch
        {
            SDFLayer.OuterShadow => -1,
            SDFLayer.InnerShadow => 1,
            SDFLayer.Outline => 2,
            _ => 0
        };

        // The solid middle gets its own quad that skips the distance math. Frames are empty there.
        private void AddFill(VertexHelper vh, UIVertex geometry, Rect rect, float inset)
        {
            var vertex = BuildFillVertex(geometry, inset);
            vertex.color = color;
            var margin = FrameReach + 1f;
            if (!AddQuad(vh, vertex, rect, margin, HoleInset(NoFill ? FrameHoleInset(ASDFEffect.HOLE_MARGIN) : inset), out var hole) || NoFill)
                return;

            vertex.uv2.x = EncodeLayer(SDFLayer.SolidFill);
            GetQuadBounds(rect, margin, out var min, out var size);
            AddGridQuad(vh, vertex, min, size, hole);
        }

        // Quad past the rect by the margin. With an inset of 0 or more the middle is left out; returns whether it was, with its grid rect.
        private static bool AddQuad(VertexHelper vh, UIVertex vertex, Rect rect, float margin, float holeInset, out RectInt hole)
        {
            GetQuadBounds(rect, margin, out var min, out var size);
            if (holeInset < 0f || !TryGetHole(rect, min, size, holeInset, out hole))
            {
                hole = default;
                AddGridQuad(vh, vertex, min, size, new RectInt(0, 0, VertexPacking.GRID_STEPS, VertexPacking.GRID_STEPS));
                return false;
            }

            // Outer then inner corners, both bottom left, top left, top right, bottom right
            var start = vh.currentVertCount;
            AddGridVertex(vh, vertex, min, size, 0, 0);
            AddGridVertex(vh, vertex, min, size, 0, VertexPacking.GRID_STEPS);
            AddGridVertex(vh, vertex, min, size, VertexPacking.GRID_STEPS, VertexPacking.GRID_STEPS);
            AddGridVertex(vh, vertex, min, size, VertexPacking.GRID_STEPS, 0);
            AddGridVertex(vh, vertex, min, size, hole.xMin, hole.yMin);
            AddGridVertex(vh, vertex, min, size, hole.xMin, hole.yMax);
            AddGridVertex(vh, vertex, min, size, hole.xMax, hole.yMax);
            AddGridVertex(vh, vertex, min, size, hole.xMax, hole.yMin);

            for (var i = 0; i < 4; i++)
            {
                var next = (i + 1) % 4;
                vh.AddTriangle(start + i, start + next, start + 4 + next);
                vh.AddTriangle(start + 4 + next, start + 4 + i, start + i);
            }
            return true;
        }

        private static void GetQuadBounds(Rect rect, float margin, out Vector2 min, out Vector2 size)
        {
            min = new Vector2(rect.xMin - margin, rect.yMin - margin);
            size = new Vector2(rect.width + margin * 2f, rect.height + margin * 2f);
        }

        // Hole in grid steps, rounded inward so it never reaches past the inset.
        // Small holes save nothing and cost vertices, so they are skipped.
        private static bool TryGetHole(Rect rect, Vector2 min, Vector2 size, float inset, out RectInt hole)
        {
            var x0 = Mathf.CeilToInt((rect.xMin + inset - min.x) / size.x * VertexPacking.GRID_STEPS);
            var y0 = Mathf.CeilToInt((rect.yMin + inset - min.y) / size.y * VertexPacking.GRID_STEPS);
            var x1 = Mathf.FloorToInt((rect.xMax - inset - min.x) / size.x * VertexPacking.GRID_STEPS);
            var y1 = Mathf.FloorToInt((rect.yMax - inset - min.y) / size.y * VertexPacking.GRID_STEPS);
            hole = new RectInt(x0, y0, x1 - x0, y1 - y0);
            return (x1 - x0) * size.x >= MIN_HOLE_SIZE * VertexPacking.GRID_STEPS && (y1 - y0) * size.y >= MIN_HOLE_SIZE * VertexPacking.GRID_STEPS;
        }

        private static void AddGridQuad(VertexHelper vh, UIVertex vertex, Vector2 min, Vector2 size, RectInt grid)
        {
            var start = vh.currentVertCount;
            AddGridVertex(vh, vertex, min, size, grid.xMin, grid.yMin);
            AddGridVertex(vh, vertex, min, size, grid.xMin, grid.yMax);
            AddGridVertex(vh, vertex, min, size, grid.xMax, grid.yMax);
            AddGridVertex(vh, vertex, min, size, grid.xMax, grid.yMin);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }

        // The position comes from the same grid step the shader decodes, so both agree exactly
        private static void AddGridVertex(VertexHelper vh, UIVertex vertex, Vector2 min, Vector2 size, int x, int y)
        {
            vertex.position = new Vector3(min.x + size.x * x / VertexPacking.GRID_STEPS, min.y + size.y * y / VertexPacking.GRID_STEPS);
            vertex.uv0.x += VertexPacking.PackGrid(x, y);
            vh.AddVert(vertex);
        }

        // Geometry shared by every quad, matching DecodeGeometry in SDFShape.cginc:
        //   uv0.xy: grid code (added per vertex to x), width, height
        //   uv0.zw: roundness top right, bottom right, top left
        //   uv1.xy: roundness bottom left, arc apex x, arc apex y
        //   uv1.zw: arc start angle, arc sweep, arc corner radius
        private UIVertex BuildGeometryVertex(float width, float height)
        {
            var roundness = _Roundness.Resolve(width, height);
            var halfMin = Mathf.Min(width, height) * 0.5f;
            _maxCornerRadius = Mathf.Min(Mathf.Max(Mathf.Max(roundness.x, roundness.y), Mathf.Max(roundness.z, roundness.w)), halfMin);
            var arc = _activeArc;
            var arcParams = arc.ResolveShaderParams(width, height);

            var size = VertexPacking.PackTriple(0f, VertexPacking.FixedPixel(width), VertexPacking.FixedPixel(height));
            var corners = VertexPacking.PackTriple(VertexPacking.FixedPixel(roundness.x), VertexPacking.FixedPixel(roundness.y), VertexPacking.FixedPixel(roundness.z));
            var apex = VertexPacking.PackTriple(VertexPacking.FixedPixel(roundness.w), VertexPacking.FixedSignedPixel(arcParams.x), VertexPacking.FixedSignedPixel(arcParams.y));
            var angles = VertexPacking.PackArcAngles(arcParams.z, arcParams.w);
            var arcData = VertexPacking.PackTriple(angles.x, angles.y, VertexPacking.FixedPixel(arc.ShaderCornerRadius));

            var vertex = UIVertex.simpleVert;
            vertex.uv0 = new Vector4(size.x, size.y, corners.x, corners.y);
            vertex.uv1 = new Vector4(apex.x, apex.y, arcData.x, arcData.y);
            return vertex;
        }

        // uv2: layer, encoded fill + hasTexture * 32768 + texture fit * 65536, gradient row + center x * 4096, inset in 1/16 px + center y * 4096.
        // Without a texture the shader skips the sample, without a gradient the row lookup.
        private UIVertex BuildFillVertex(UIVertex geometry, float inset)
        {
            var insetCode = Mathf.Round(Mathf.Clamp(inset, 0f, VertexPacking.MAX_FILL_INSET) * 16f);
            var fill = UsesGradient ? _Fill.EncodeShaderFill() : 0f;
            var textureFlag = _Texture ? 32768f + (int) _TextureFit * 65536f : 0f;
            var center = _Fill.EncodeShaderCenter();
            var row = Mathf.Clamp(_gradientRow, 0, 4095);
            geometry.uv2 = new Vector4(EncodeLayer(SDFLayer.Fill), fill + textureFlag, row + center.x * 4096f, insetCode + center.y * 4096f);
            return geometry;
        }

        // Matches DecodeLayer in SDFShape.cginc:
        // frameWidth * 16 + framePlacement * 16384 + frameMode * 65536 + cornerShape * 131072 + sharpApex * 1048576 + layer * 2097152
        private float EncodeLayer(SDFLayer layer)
        {
            var frame = NoFill ? FrameWidthQuantized * 16f + (int) _FramePlacement * 16384f + 65536f : 0f;
            var sharpApex = _activeArc.SharpCenter ? 1048576f : 0f;
            return frame + EncodedCornerShape * 131072f + sharpApex + (int) layer * 2097152f;
        }

        private void MigrateLegacyFill()
        {
            var tint = color;
            if (_Fill.TryMigrateLegacy(ref tint))
                color = tint;
        }

        private void EnsureShader()
        {
            if (!_Shader)
                _Shader = Shader.Find(SHADER_NAME);
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

    public interface ICornerStyleProvider
    {
        CornerStyle CornerStyle { get; }
    }

    // Ranges the vertex encodings can hold; property setters clamp to them before the value is stored.
    // Everything travels in the uv channels, the only vertex data the canvas leaves untransformed.
    // Layouts match the decode functions in SDFShape.cginc.
    public static class VertexPacking
    {
        public const float MAX_PIXEL = 4095f;
        public const float MAX_OFFSET = 512f;
        public const float MAX_SIGNED_PIXEL = 2048f;
        public const float MIN_SHADOW_POWER = 0.01f;
        public const float MAX_SHADOW_POWER = 255.99f;
        public const float MAX_RADIAL_SIZE = 40.95f;
        public const float MAX_FILL_INSET = 255f;
        public const float MAX_FRAME_WIDTH = 1023.9375f;
        public const float MAX_SHADOW_PIXEL = 1023.75f;

        private const float FIXED_MAX = 65535f;
        private const float FIXED_STEP = 16f;
        private const float OFFSET_STEP = 4f;
        private const float FULL_TURN = Mathf.PI * 2f;

        public static float ClampPixel(float value) => Mathf.Clamp(value, 0f, MAX_PIXEL);
        public static float ClampShadowPower(float value) => Mathf.Clamp(value, MIN_SHADOW_POWER, MAX_SHADOW_POWER);
        public static float ClampRadialSize(float value) => Mathf.Clamp(value, 0f, MAX_RADIAL_SIZE);
        public static float ClampFrameWidth(float value) => Mathf.Clamp(value, 0f, MAX_FRAME_WIDTH);
        public static float ClampShadowPixel(float value) => Mathf.Clamp(value, 0f, MAX_SHADOW_PIXEL);

        // Frame width in 1/16 px, shadow size and blur in 1/4 px
        public static float QuantizeFrameWidth(float value) => Mathf.Round(ClampFrameWidth(value) * 16f) / 16f;
        public static float QuantizeShadowPixel(float value) => Mathf.Round(ClampShadowPixel(value) * 4f) / 4f;

        public static float PackBytes(int b0, int b1, int b2) => b0 + b1 * 256f + b2 * 65536f;

        // x = rgb, y = a
        public static Vector2 PackColor(Color color)
        {
            Color32 c = color;
            return new Vector2(PackBytes(c.r, c.g, c.b), c.a);
        }

        // 16-bit fixed point in 1/16 px steps, up to 4095.9375 px
        public static float FixedPixel(float value) => Mathf.Clamp(Mathf.Round(value * FIXED_STEP), 0f, FIXED_MAX);

        // 16-bit fixed point, ±2048 px in 1/16 px steps
        public static float FixedSignedPixel(float value) => Mathf.Clamp(Mathf.Round((value + MAX_SIGNED_PIXEL) * FIXED_STEP), 0f, FIXED_MAX);

        // 16-bit turn fraction, 65535 is a full turn
        public static float FixedAngle(float radians) => Mathf.Clamp(Mathf.Round(radians / FULL_TURN * FIXED_MAX), 0f, FIXED_MAX);

        // Three 16-bit values in two floats: a with the low byte of b, then the high byte of b with c
        public static Vector2 PackTriple(float a, float b, float c)
        {
            var bLow = b % 256f;
            var bHigh = (b - bLow) / 256f;
            return new Vector2(a + bLow * 65536f, bHigh + c * 256f);
        }

        public const int GRID_STEPS = 255;

        // Vertex position over its quad in 1/255 steps, 8 bits per axis
        public static float PackGrid(int x, int y) => x + y * 256f;

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
    [CustomEditor(typeof(SDFGraphic), true)]
    [CanEditMultipleObjects]
    public class SDFGraphicEditor : NativeWrapperOdinEditor<MaskableGraphic, GraphicEditor>
    {
        protected override BaseEditorDrawMode BaseEditorDrawMode => BaseEditorDrawMode.Hidden;
    }

    // Prefab stages open with their own canvases, which need the extra channels before the graphics can draw
    [InitializeOnLoad]
    public static class SDFShapesPrefabStageRefresher
    {
        private static GameObject _queuedRoot;
        private static int _queuedPasses;
        private static bool _refreshQueued;

        static SDFShapesPrefabStageRefresher()
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
                canvas.additionalShaderChannels |= SDFGraphic.REQUIRED_CANVAS_CHANNELS;
                if (canvas.rootCanvas)
                    canvas.rootCanvas.additionalShaderChannels |= SDFGraphic.REQUIRED_CANVAS_CHANNELS;
            }

            foreach (var graphic in root.GetComponentsInChildren<SDFGraphic>(true))
                graphic.Refresh();

            Canvas.ForceUpdateCanvases();
            SceneView.RepaintAll();
        }
    }
#endif
}
