// Author: Michal Petr
// Created: 22.09.2026

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.ProceduralUI
{
    public class SignedDistanceFieldGraphic : AProceduralGraphic, ICanvasRaycastFilter
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
        [LabelText("Size")]
        [MinValue(0f)]
        [SerializeField]
        private float _OutlineSize;

        [EnhancedBoxGroup("Outline")]
        [EnableIf(nameof(HasOutline))]
        [LabelText("Color")]
        [SerializeField]
        private Color _OutlineColor = Color.black;

        [EnhancedBoxGroup("Outline")]
        [EnableIf(nameof(HasOutline))]
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
        [LabelText("Size")]
        [MinValue(0f)]
        [SerializeField]
        private float _ShadowSize;

        [EnhancedBoxGroup("Shadow")]
        [EnableIf(nameof(HasShadow))]
        [LabelText("Color")]
        [SerializeField]
        private Color _ShadowColor = Color.black;

        [EnhancedBoxGroup("Shadow")]
        [EnableIf(nameof(HasShadow))]
        [LabelText("Blur")]
        [MinValue(0f)]
        [SerializeField]
        private float _ShadowBlur;

        [EnhancedBoxGroup("Shadow")]
        [EnableIf(nameof(HasShadow))]
        [LabelText("Power")]
        [MinValue(0f)]
        [SerializeField]
        private float _ShadowPower = 1f;

        [EnhancedBoxGroup("Shadow")]
        [LabelText("Offset")]
        [SerializeField]
        private Vector2 _ShadowOffset;

        [EnhancedBoxGroup("Arc", order: 40, SpaceAfter = 4)]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private ArcCutout _Arc = ArcCutout.Default;

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

        public float OutlineSize
        {
            get => _OutlineSize;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_OutlineSize, value)) return; _OutlineSize = value; SetVerticesDirty(); }
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

        public float ShadowSize
        {
            get => _ShadowSize;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_ShadowSize, value)) return; _ShadowSize = value; SetVerticesDirty(); }
        }

        public float ShadowBlur
        {
            get => _ShadowBlur;
            set { value = VertexPacking.ClampPixel(value); if (Mathf.Approximately(_ShadowBlur, value)) return; _ShadowBlur = value; SetVerticesDirty(); }
        }

        public float ShadowPower
        {
            get => _ShadowPower;
            set { value = VertexPacking.ClampShadowPower(value); if (Mathf.Approximately(_ShadowPower, value)) return; _ShadowPower = value; SetVerticesDirty(); }
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

        public ArcCutout Arc
        {
            get => _Arc;
            set { _Arc = value; SetVerticesDirty(); }
        }

        public float ArcValue
        {
            get => _Arc.Value;
            set { if (Mathf.Approximately(_Arc.Value, value)) return; _Arc.Value = value; SetVerticesDirty(); }
        }

        public override Texture mainTexture => _Texture ? _Texture : s_WhiteTexture;
        protected override string ShaderName => SHADER_NAME;
        protected override CornerJoin ResolvedCornerJoin => _CornerJoin;

        private bool HasOutline => _OutlineSize > 0f;
        private bool HasShadow => _ShadowSize > 0f || _ShadowBlur > 0f || _ShadowOffset != Vector2.zero;
        private float ShadowSizeRounded => VertexPacking.RoundPixel(_ShadowSize);
        private float ShadowBlurRounded => VertexPacking.RoundPixel(_ShadowBlur);
        private float ShadowOffsetExtent => Mathf.Max(Mathf.Abs(VertexPacking.QuantizeOffset(_ShadowOffset.x)), Mathf.Abs(VertexPacking.QuantizeOffset(_ShadowOffset.y)));
        private float ExtraMargin => OutlineOutwardExtension + FrameOutwardExtension + ShadowSizeRounded + ShadowBlurRounded + ShadowOffsetExtent + 1f;

        private float OutlineOutwardExtension => _OutlinePlacement switch
        {
            EdgePlacement.Center => VertexPacking.ClampPixel(_OutlineSize) * 0.5f,
            EdgePlacement.Outside => VertexPacking.ClampPixel(_OutlineSize),
            _ => 0f
        };

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (_Arc.IsFull)
                return true;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local))
                return false;

            var rect = rectTransform.rect;
            return _Arc.Contains(local - rect.center, rect.width, rect.height);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var width = rectTransform.rect.width;
            var height = rectTransform.rect.height;

            if (width <= 0f || height <= 0f || _Arc.IsEmpty)
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

            vertex.position = new Vector3(-margin, -margin) - pivot;
            vertex.uv0 = new Vector4(0f, 0f, width, height);
            vh.AddVert(vertex);

            vertex.position = new Vector3(-margin, height + margin) - pivot;
            vertex.uv0 = new Vector4(0f, 1f, width, height);
            vh.AddVert(vertex);

            vertex.position = new Vector3(width + margin, height + margin) - pivot;
            vertex.uv0 = new Vector4(1f, 1f, width, height);
            vh.AddVert(vertex);

            vertex.position = new Vector3(width + margin, -margin) - pivot;
            vertex.uv0 = new Vector4(1f, 0f, width, height);
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
                    vertex.uv0 = new Vector4(fx, fy, width, height);
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

        // Effect info is packed into existing channels:
        //   uv2.x: outlineSize + outlinePlacement * 4096 + framePlacement * 16384 + cornerShape * 65536, negated and offset by 1 in frame mode
        //   uv2.y: round(shadowSize) + round(shadowBlur) * 4096
        //   uv2.z: shadow offset x as 16-bit fixed point, one byte spare
        //   uv2.w: shadowPower + round(frameWidth) * 256
        //   uv3: outline, shadow and gradient color, three bytes per float
        //   tangent: arc apex (xy) and start / end angle (zw)
        //   normal: arc corner radius (x), shadow offset y as 16-bit fixed point with one byte spare (y), encoded fill + sharp center flag (z)
        private UIVertex BuildBaseVertex(float width, float height)
        {
            var frameWidthRounded = NoFill ? VertexPacking.RoundPixel(FrameWidth) : 0f;
            var outlineInfo = VertexPacking.ClampPixel(_OutlineSize) + (int) _OutlinePlacement * 4096f + (int) FramePlacement * 16384f + EncodedCornerShape * 65536f;
            var encodedOutline = NoFill ? -(1f + outlineInfo) : outlineInfo;
            var encodedShadow = ShadowSizeRounded + ShadowBlurRounded * 4096f;
            var encodedShadowPower = VertexPacking.ClampShadowPower(_ShadowPower) + frameWidthRounded * 256f;

            var vertex = UIVertex.simpleVert;
            vertex.normal = new Vector3(_Arc.ShaderCornerRadius, VertexPacking.PackOffset(_ShadowOffset.y), _Fill.EncodeShaderFill() + _Arc.ShaderSharpCenterFlag);
            vertex.tangent = _Arc.ResolveShaderParams(width, height);
            vertex.uv1 = GetRoundness();
            vertex.uv2 = new Vector4(encodedOutline, encodedShadow, VertexPacking.PackOffset(_ShadowOffset.x), encodedShadowPower);
            vertex.uv3 = VertexPacking.PackColors(_OutlineColor, _ShadowColor, _Fill.GetGradientColor(color));
            return vertex;
        }
    }
}
