// Author: Michal Petr
// Created: 22.09.2026

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.ProceduralUI
{
    public class SDFGraphic : AProceduralGraphic, ICanvasRaycastFilter
    {
        private const string SHADER_NAME = "Hidden/EDIVE/ProceduralUI/SimpleSDF";

        [PropertyOrder(0)]
        [SerializeField]
        private Texture _Texture;

        [PropertyOrder(1)]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private GradientFill _Fill = GradientFill.Default;

        [EnhancedBoxGroup("Outline", order: 20, SpaceBefore = 4, SpaceAfter = 4)]
        [ShowInGroupHeader]
        [HideLabel]
        [SerializeField]
        private bool _OutlineEnabled;

        [SerializeField]
        [HideInInspector]
        private float _OutlineSize;

        [EnhancedBoxGroup("Outline")]
        [PropertyOrder(0)]
        [ShowIf(nameof(_OutlineEnabled))]
        [LabelText("Size")]
        [ShowInInspector]
        public float OutlineSize
        {
            get => _OutlineSize;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_OutlineSize, value)) return; _OutlineSize = value; SetVerticesDirty(); }
        }

        [EnhancedBoxGroup("Outline")]
        [PropertyOrder(1)]
        [ShowIf(nameof(_OutlineEnabled))]
        [LabelText("Color")]
        [SerializeField]
        private Color _OutlineColor = Color.black;

        [EnhancedBoxGroup("Outline")]
        [PropertyOrder(2)]
        [ShowIf(nameof(_OutlineEnabled))]
        [LabelText("Placement")]
        [SerializeField]
        private EdgePlacement _OutlinePlacement = EdgePlacement.Outside;

        [PropertySpace]
        [PropertyOrder(19)]
        [LabelText("Corner Style")]
        [IconEnumToggleButtons]
        [SerializeField]
        private CornerJoin _CornerJoin;

        [EnhancedBoxGroup("Shadow", order: 30, SpaceAfter = 4)]
        [ShowInGroupHeader]
        [HideLabel]
        [SerializeField]
        private bool _ShadowEnabled;

        [SerializeField]
        [HideInInspector]
        private float _ShadowSize;

        [EnhancedBoxGroup("Shadow")]
        [PropertyOrder(0)]
        [ShowIf(nameof(_ShadowEnabled))]
        [LabelText("Size")]
        [ShowInInspector]
        public float ShadowSize
        {
            get => _ShadowSize;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_ShadowSize, value)) return; _ShadowSize = value; SetVerticesDirty(); }
        }

        [EnhancedBoxGroup("Shadow")]
        [PropertyOrder(1)]
        [ShowIf(nameof(_ShadowEnabled))]
        [LabelText("Color")]
        [SerializeField]
        private Color _ShadowColor = Color.black;

        [SerializeField]
        [HideInInspector]
        private float _ShadowBlur;

        [EnhancedBoxGroup("Shadow")]
        [PropertyOrder(2)]
        [ShowIf(nameof(_ShadowEnabled))]
        [LabelText("Blur")]
        [ShowInInspector]
        public float ShadowBlur
        {
            get => _ShadowBlur;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_ShadowBlur, value)) return; _ShadowBlur = value; SetVerticesDirty(); }
        }

        [SerializeField]
        [HideInInspector]
        private float _ShadowPower = 1f;

        [EnhancedBoxGroup("Shadow")]
        [PropertyOrder(3)]
        [ShowIf(nameof(_ShadowEnabled))]
        [LabelText("Power")]
        [ShowInInspector]
        public float ShadowPower
        {
            get => _ShadowPower;
            set { value = VertexPacking.ClampShadowPower(value); if (Mathf.Approximately(_ShadowPower, value)) return; _ShadowPower = value; SetVerticesDirty(); }
        }

        [EnhancedBoxGroup("Shadow")]
        [PropertyOrder(4)]
        [ShowIf(nameof(_ShadowEnabled))]
        [LabelText("Offset")]
        [SerializeField]
        private Vector2 _ShadowOffset;

        [EnhancedBoxGroup("Shadow")]
        [PropertyOrder(5)]
        [ShowIf(nameof(_ShadowEnabled))]
        [LabelText("Inset")]
        [SerializeField]
        private bool _ShadowInset;

        [EnhancedBoxGroup("Arc", order: 40, SpaceAfter = 4)]
        [ShowInGroupHeader]
        [HideLabel]
        [SerializeField]
        private bool _ArcEnabled;

        [EnhancedBoxGroup("Arc")]
        [ShowIf(nameof(_ArcEnabled))]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private ArcCutout _Arc = ArcCutout.Default;
        
        [PropertySpace]
        [PropertyOrder(10)]
        [SerializeField]
        private CornerRoundness _Roundness;

        public Texture Texture
        {
            get => _Texture;
            set { if (_Texture == value) return; _Texture = value; SetVerticesDirty(); SetMaterialDirty(); }
        }

        public GradientFill Fill
        {
            get => _Fill;
            set { _Fill = value; SetVerticesDirty(); }
        }

        public bool OutlineEnabled
        {
            get => _OutlineEnabled;
            set { if (_OutlineEnabled == value) return; _OutlineEnabled = value; SetVerticesDirty(); }
        }

        public Color OutlineColor
        {
            get => _OutlineColor;
            set { if (_OutlineColor == value) return; _OutlineColor = value; SetVerticesDirty(); }
        }

        public EdgePlacement OutlinePlacement
        {
            get => _OutlinePlacement;
            set { if (_OutlinePlacement == value) return; _OutlinePlacement = value; SetVerticesDirty(); }
        }

        public CornerJoin CornerJoin
        {
            get => _CornerJoin;
            set { if (_CornerJoin == value) return; _CornerJoin = value; SetVerticesDirty(); }
        }

        public bool ShadowEnabled
        {
            get => _ShadowEnabled;
            set { if (_ShadowEnabled == value) return; _ShadowEnabled = value; SetVerticesDirty(); }
        }

        public Color ShadowColor
        {
            get => _ShadowColor;
            set { if (_ShadowColor == value) return; _ShadowColor = value; SetVerticesDirty(); }
        }

        public Vector2 ShadowOffset
        {
            get => _ShadowOffset;
            set { if (_ShadowOffset == value) return; _ShadowOffset = value; SetVerticesDirty(); }
        }

        public bool ShadowInset
        {
            get => _ShadowInset;
            set { if (_ShadowInset == value) return; _ShadowInset = value; SetVerticesDirty(); }
        }

        public bool ArcEnabled
        {
            get => _ArcEnabled;
            set { if (_ArcEnabled == value) return; _ArcEnabled = value; SetVerticesDirty(); }
        }

        public ArcCutout Arc
        {
            get => _Arc;
            set { _Arc = value; SetVerticesDirty(); }
        }

        public ArcCutout ActiveArc => _ArcEnabled ? _Arc : ArcCutout.Default;

        public float ArcValue
        {
            get => _Arc.Value;
            set { if (Mathf.Approximately(_Arc.Value, value)) return; _Arc.Value = value; SetVerticesDirty(); }
        }
        
        public CornerRoundness Roundness
        {
            get => _Roundness;
            set { _Roundness = value; SetVerticesDirty(); }
        }

        public override Texture mainTexture => _Texture ? _Texture : s_WhiteTexture;
        protected override string ShaderName => SHADER_NAME;
        protected override CornerJoin ResolvedCornerJoin => _CornerJoin;

        private float ActiveOutlineSize => _OutlineEnabled ? _OutlineSize : 0f;
        private Vector2 ActiveShadowOffset => _ShadowEnabled ? _ShadowOffset : Vector2.zero;
        private float ShadowSizeRounded => _ShadowEnabled ? Mathf.Round(_ShadowSize) : 0f;
        private float ShadowBlurRounded => _ShadowEnabled ? Mathf.Round(_ShadowBlur) : 0f;
        private float ShadowOffsetExtent => Mathf.Max(Mathf.Abs(VertexPacking.QuantizeOffset(ActiveShadowOffset.x)), Mathf.Abs(VertexPacking.QuantizeOffset(ActiveShadowOffset.y)));
        private float ShadowOutwardExtension => _ShadowInset ? 0f : ShadowSizeRounded + ShadowBlurRounded + ShadowOffsetExtent;
        private float ExtraMargin => OutlineOutwardExtension + FrameOutwardExtension + ShadowOutwardExtension + 1f;

        private float OutlineOutwardExtension => _OutlinePlacement switch
        {
            EdgePlacement.Center => ActiveOutlineSize * 0.5f,
            EdgePlacement.Outside => ActiveOutlineSize,
            _ => 0f
        };

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var arc = ActiveArc;
            if (arc.IsFull)
                return true;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local))
                return false;

            var rect = rectTransform.rect;
            return arc.Contains(local - rect.center, rect.width, rect.height);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var width = rectTransform.rect.width;
            var height = rectTransform.rect.height;

            if (width <= 0f || height <= 0f || ActiveArc.IsEmpty)
                return;

            if (_Fill.RequiresSubdivision)
            {
                PopulateSubdividedMesh(vh, width, height);
                return;
            }

            var margin = ExtraMargin;
            var pivot = new Vector3(rectTransform.pivot.x * width, rectTransform.pivot.y * height, 0f);

            var vertex = BuildBaseVertex(width, height);
            vertex.color = _Fill.GetVertexColor(color);
            var sizeCode = vertex.uv0.x;

            vertex.position = new Vector3(-margin, -margin) - pivot;
            vertex.uv0.x = sizeCode + VertexPacking.PackGrid(0, 0, 1);
            vh.AddVert(vertex);

            vertex.position = new Vector3(-margin, height + margin) - pivot;
            vertex.uv0.x = sizeCode + VertexPacking.PackGrid(0, 1, 1);
            vh.AddVert(vertex);

            vertex.position = new Vector3(width + margin, height + margin) - pivot;
            vertex.uv0.x = sizeCode + VertexPacking.PackGrid(1, 1, 1);
            vh.AddVert(vertex);

            vertex.position = new Vector3(width + margin, -margin) - pivot;
            vertex.uv0.x = sizeCode + VertexPacking.PackGrid(1, 0, 1);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private void PopulateSubdividedMesh(VertexHelper vh, float width, float height)
        {
            var n = _Fill.GradientQuality;
            var margin = ExtraMargin;
            var pivot = new Vector3(rectTransform.pivot.x * width, rectTransform.pivot.y * height, 0f);
            var tint = color;

            var vertex = BuildBaseVertex(width, height);
            var sizeCode = vertex.uv0.x;

            var posMinX = -margin;
            var posRangeX = width + margin * 2f;
            var posMinY = -margin;
            var posRangeY = height + margin * 2f;

            var cols = n + 1;
            for (var y = 0; y <= n; y++)
            {
                var fy = (float) y / n;
                var posY = posMinY + posRangeY * fy - pivot.y;

                for (var x = 0; x <= n; x++)
                {
                    var fx = (float) x / n;
                    vertex.position = new Vector3(posMinX + posRangeX * fx - pivot.x, posY, 0f);
                    vertex.uv0.x = sizeCode + VertexPacking.PackGrid(x, y, n);
                    vertex.color = _Fill.Evaluate(fx, fy, width, height, tint);
                    vh.AddVert(vertex);
                }
            }

            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = y * cols + x;
                    vh.AddTriangle(i, i + cols, i + cols + 1);
                    vh.AddTriangle(i + cols + 1, i + 1, i);
                }
            }
        }
        
        public Vector4 ResolveRoundness(float width, float height) => _Roundness.Resolve(width, height);
        
        protected override Vector4 GetRoundness() => ResolveRoundness(rectTransform.rect.width, rectTransform.rect.height);

        // Effect info on top of the shared geometry in uv0 and uv1 (see AProceduralGraphic.PackGeometry):
        //   uv2.x: outlineSize + outlinePlacement * 4096 + framePlacement * 16384 + cornerShape * 65536, negated and offset by 1 in frame mode
        //   uv2.y: round(shadowSize) + round(shadowBlur) * 4096, negated and offset by 1 when inset
        //   uv2.z: shadow offset x and y, 12 bits each
        //   uv2.w: shadowPower + round(frameWidth) * 256
        //   uv3: outline, shadow and gradient color, three bytes per float
        //   tangent.w: encoded fill + sharp center flag
        private UIVertex BuildBaseVertex(float width, float height)
        {
            var frameWidthRounded = NoFill ? Mathf.Round(FrameWidth) : 0f;
            var outlineInfo = ActiveOutlineSize + (int) _OutlinePlacement * 4096f + (int) FramePlacement * 16384f + EncodedCornerShape * 65536f;
            var encodedOutline = NoFill ? -(1f + outlineInfo) : outlineInfo;
            var shadowInfo = ShadowSizeRounded + ShadowBlurRounded * 4096f;
            var encodedShadow = _ShadowEnabled && _ShadowInset ? -(1f + shadowInfo) : shadowInfo;
            var encodedShadowPower = _ShadowPower + frameWidthRounded * 256f;
            var arc = ActiveArc;

            var vertex = UIVertex.simpleVert;
            PackGeometry(width, height, GetRoundness(), arc.ResolveShaderParams(width, height), arc.ShaderCornerRadius, out vertex.uv0, out vertex.uv1);
            vertex.tangent = new Vector4(0f, 0f, 0f, _Fill.EncodeShaderFill() + arc.ShaderSharpCenterFlag);
            vertex.uv2 = new Vector4(encodedOutline, encodedShadow, VertexPacking.PackOffsets(ActiveShadowOffset), encodedShadowPower);
            vertex.uv3 = VertexPacking.PackColors(_OutlineColor, _ShadowColor, _Fill.GetGradientColor(color));
            return vertex;
        }
    }
}
