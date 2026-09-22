// Author: Michal Petr
// Created: 22.09.2026

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EDIVE.UIElements.ProceduralUI
{
    public class GlowGraphic : AProceduralGraphic, ILayoutIgnorer
    {
        internal const string SHADER_NAME = "Hidden/EDIVE/ProceduralUI/Glow";

        [PropertyOrder(-10)]
        [FormerlySerializedAs("_SourceRectangle")]
        [SerializeField]
        private SimpleSDFGraphic _Source;

        [PropertyOrder(-9)]
        [SerializeField]
        private float _ExtraSize;

        [PropertySpace]
        [PropertyOrder(0)]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private GradientFill _Fill = GradientFill.Default;

        [PropertySpace]
        [PropertyOrder(10)]
        [ShowIf(nameof(HasOwnShape))]
        [SerializeField]
        private CornerRoundness _Roundness;

        [PropertySpace]
        [PropertyOrder(20)]
        [MinValue(0f)]
        [SerializeField]
        private float _Spread;

        [PropertyOrder(21)]
        [MinValue(0f)]
        [SerializeField]
        private float _Blur = 10f;

        [PropertyOrder(22)]
        [MinValue(0.01f)]
        [SerializeField]
        private float _Power = 1f;

        [SerializeField]
        [HideInInspector]
        private GlowModifier _Owner;

        private SimpleSDFGraphic _trackedSource;

        internal GlowModifier Owner
        {
            get => _Owner;
            set => _Owner = value;
        }

        public SimpleSDFGraphic Source
        {
            get => _Source;
            set
            {
                if (_Source != value)
                {
                    _Source = value;
                    SetVerticesDirty();
                }
                TrackSource();
            }
        }

        public float ExtraSize
        {
            get => _ExtraSize;
            set { if (Mathf.Approximately(_ExtraSize, value)) return; _ExtraSize = value; SetVerticesDirty(); }
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

        public float Spread
        {
            get => _Spread;
            set { value = Mathf.Max(0f, value); if (Mathf.Approximately(_Spread, value)) return; _Spread = value; SetVerticesDirty(); }
        }

        public float Blur
        {
            get => _Blur;
            set { value = Mathf.Max(0f, value); if (Mathf.Approximately(_Blur, value)) return; _Blur = value; SetVerticesDirty(); }
        }

        public float Power
        {
            get => _Power;
            set { value = Mathf.Max(0.01f, value); if (Mathf.Approximately(_Power, value)) return; _Power = value; SetVerticesDirty(); }
        }

        public bool ignoreLayout => true;
        protected override string ShaderName => SHADER_NAME;
        protected override bool HasOwnShape => !_Source;

        protected override void OnEnable()
        {
            base.OnEnable();
            TrackSource();
        }

        protected override void OnDisable()
        {
            if (_trackedSource)
                _trackedSource.UnregisterDirtyVerticesCallback(OnSourceChanged);
            _trackedSource = null;
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            TrackSource();
        }
#endif

        private void TrackSource()
        {
            if (_trackedSource == _Source)
                return;

            if (_trackedSource)
                _trackedSource.UnregisterDirtyVerticesCallback(OnSourceChanged);

            _trackedSource = _Source;

            if (_trackedSource)
                _trackedSource.RegisterDirtyVerticesCallback(OnSourceChanged);
        }

        private void OnSourceChanged()
        {
            SetVerticesDirty();
        }

        protected override Vector4 GetRoundness()
        {
            if (_Source)
            {
                var sourceWidth = _Source.rectTransform.rect.width + _ExtraSize * 2f;
                var sourceHeight = _Source.rectTransform.rect.height + _ExtraSize * 2f;
                return _Source.ResolveRoundness(sourceWidth, sourceHeight);
            }

            return _Roundness.Resolve(rectTransform.rect.width, rectTransform.rect.height);
        }

        private void GetEffectiveDimensions(out float width, out float height)
        {
            var rect = _Source ? _Source.rectTransform.rect : rectTransform.rect;
            width = rect.width + _ExtraSize * 2f;
            height = rect.height + _ExtraSize * 2f;
        }

        protected override ShapeStyle ResolvedShapeStyle => _Source ? _Source.ShapeStyle : ShapeStyle;
        protected override CornerJoin ResolvedCornerJoin => _Source ? _Source.CornerJoin : CornerJoin.Round;

        // uv2.w: cornerShape * 65536 + frame, where frame is 0 when filled, else 1 + placement * 4096 + round(frameWidth)
        private void GetFrameEncoding(out float encodedFrame, out float outwardExtension)
        {
            var noFill = _Source ? _Source.NoFill : NoFill;
            var frameWidth = _Source ? _Source.FrameWidth : FrameWidth;
            var placement = _Source ? _Source.FramePlacement : FramePlacement;

            encodedFrame = EncodedCornerShape * 65536f;
            outwardExtension = 0f;
            if (!noFill)
                return;

            encodedFrame += 1f + (int) placement * 4096f + VertexPacking.RoundPixel(frameWidth);
            outwardExtension = ResolveFrameOutwardExtension(true, frameWidth, placement);
        }

        // The arc is the source rectangle's; a glow with its own shape has none
        private ArcCutout SourceArc => _Source ? _Source.Arc : default;

        // The arc apex is relative to the source rect, not the enlarged glow rect
        private UIVertex BuildBaseVertex()
        {
            var arc = SourceArc;
            var rect = _Source ? _Source.rectTransform.rect : rectTransform.rect;

            var vertex = UIVertex.simpleVert;
            vertex.normal = new Vector3(arc.ShaderCornerRadius, 0f, _Fill.EncodeShaderFill() + arc.ShaderSharpCenterFlag);
            vertex.tangent = arc.ResolveShaderParams(rect.width, rect.height);
            vertex.uv1 = GetRoundness();
            vertex.uv3 = VertexPacking.PackColor(_Fill.GetGradientColor(color));
            return vertex;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (rectTransform.rect.width <= 0f || rectTransform.rect.height <= 0f || SourceArc.IsEmpty)
                return;

            GetEffectiveDimensions(out var width, out var height);

            if (_Fill.RequiresSubdivision)
                PopulateGlowSubdivided(vh, width, height);
            else
                PopulateGlowQuad(vh, width, height);
        }

        private void PopulateGlowQuad(VertexHelper vh, float width, float height)
        {
            GetFrameEncoding(out var encodedFrame, out var frameExtension);
            var margin = _Spread + _Blur + frameExtension + 1f;

            var rectWidth = rectTransform.rect.width;
            var rectHeight = rectTransform.rect.height;
            var offsetX = (rectWidth - width) * 0.5f;
            var offsetY = (rectHeight - height) * 0.5f;
            var pivot = new Vector3(rectTransform.pivot.x * rectWidth, rectTransform.pivot.y * rectHeight, 0f);

            var vertex = BuildBaseVertex();
            vertex.uv2 = new Vector4(_Spread, _Blur, _Power, encodedFrame);
            vertex.color = _Fill.GetVertexColor(color);

            vertex.position = new Vector3(offsetX - margin, offsetY - margin) - pivot;
            vertex.uv0 = new Vector4(0f, 0f, width, height);
            vh.AddVert(vertex);

            vertex.position = new Vector3(offsetX - margin, offsetY + height + margin) - pivot;
            vertex.uv0 = new Vector4(0f, 1f, width, height);
            vh.AddVert(vertex);

            vertex.position = new Vector3(offsetX + width + margin, offsetY + height + margin) - pivot;
            vertex.uv0 = new Vector4(1f, 1f, width, height);
            vh.AddVert(vertex);

            vertex.position = new Vector3(offsetX + width + margin, offsetY - margin) - pivot;
            vertex.uv0 = new Vector4(1f, 0f, width, height);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private void PopulateGlowSubdivided(VertexHelper vh, float width, float height)
        {
            var n = _Fill.GradientQuality;
            GetFrameEncoding(out var encodedFrame, out var frameExtension);
            var margin = _Spread + _Blur + frameExtension + 1f;

            var rectWidth = rectTransform.rect.width;
            var rectHeight = rectTransform.rect.height;
            var offsetX = (rectWidth - width) * 0.5f;
            var offsetY = (rectHeight - height) * 0.5f;
            var pivot = new Vector3(rectTransform.pivot.x * rectWidth, rectTransform.pivot.y * rectHeight, 0f);
            var tint = color;

            var posMinX = offsetX - margin;
            var posRangeX = width + margin * 2f;
            var posMinY = offsetY - margin;
            var posRangeY = height + margin * 2f;

            var vertex = BuildBaseVertex();
            vertex.uv2 = new Vector4(_Spread, _Blur, _Power, encodedFrame);

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
    }
}
