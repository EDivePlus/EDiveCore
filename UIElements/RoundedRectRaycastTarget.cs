using EDIVE.DataStructures;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using EDIVE.OdinExtensions.Editor;
#endif

namespace EDIVE.UIElements
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class RoundedRectRaycastTarget : MaskableGraphic, ICanvasRaycastFilter
    {
        [SerializeField] 
        private bool _UseRadiusPercentage;
        
        [Min(0f)]
        [HideIf(nameof(_UseRadiusPercentage))]
        [SerializeField] 
        private float _Radius;
        
        [Range(0f, 0.5f)]
        [ShowIf(nameof(_UseRadiusPercentage))]
        [SerializeField] 
        private float _RadiusPercent;
        
        [ShowInInspector]
        public RectPadding RaycastPadding
        {
            get => raycastPadding;
            set => raycastPadding = value;
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
            maskable = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = true;
            maskable = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        private float ResolveRadius(Vector2 half)
        {
            var maxR = Mathf.Min(half.x, half.y);
            return Mathf.Clamp(_UseRadiusPercentage ? _RadiusPercent * 2f * maxR : _Radius, 0f, maxR);
        }

        private Rect GetPaddedRect()
        {
            var r = rectTransform.rect;
            return new Rect(
                r.x + RaycastPadding.Left,
                r.y + RaycastPadding.Bottom,
                Mathf.Max(0f, r.width - RaycastPadding.Left - RaycastPadding.Right),
                Mathf.Max(0f, r.height - RaycastPadding.Bottom - RaycastPadding.Top));
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPoint, eventCamera, out var local))
                return false;

            var r = GetPaddedRect();
            if (r.width <= 0f || r.height <= 0f) return false;
            
            var p = local - r.center;
            var half = r.size * 0.5f;
            var radius = ResolveRadius(half);

            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (half - Vector2.one * radius);
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
            var inside  = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
            return outside + inside - radius <= 0f;
        }

#if UNITY_EDITOR
        private static readonly Color GIZMO_COLOR = Color.yellow.WithA(0.2f);
        private const int GIZMO_CORNER_SEGMENTS = 16;
        
        protected override void OnValidate()
        {
            base.OnValidate();
            _Radius = Mathf.Max(0f, _Radius);
            _RadiusPercent = Mathf.Clamp(_RadiusPercent, 0f, 0.5f);
            raycastTarget = true;
            maskable = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (rectTransform == null) 
                return;

            var r = GetPaddedRect();
            if (r.width <= 0f || r.height <= 0f) return;
            
            var half = r.size * 0.5f;
            var center = r.center;
            var radius = Mathf.Max(0f, ResolveRadius(half));

            var prevMatrix = Gizmos.matrix;
            var prevColor = Gizmos.color;
            Gizmos.matrix = rectTransform.localToWorldMatrix;
            Gizmos.color = GIZMO_COLOR;

            if (radius <= 0.0001f)
            {
                var bl = new Vector3(center.x - half.x, center.y - half.y, 0f);
                var br = new Vector3(center.x + half.x, center.y - half.y, 0f);
                var tr = new Vector3(center.x + half.x, center.y + half.y, 0f);
                var tl = new Vector3(center.x - half.x, center.y + half.y, 0f);
                Gizmos.DrawLine(bl, br);
                Gizmos.DrawLine(br, tr);
                Gizmos.DrawLine(tr, tl);
                Gizmos.DrawLine(tl, bl);
            }
            else
            {
                var inset = half - Vector2.one * radius;
                var cBL = center + new Vector2(-inset.x, -inset.y);
                var cBR = center + new Vector2( inset.x, -inset.y);
                var cTR = center + new Vector2( inset.x,  inset.y);
                var cTL = center + new Vector2(-inset.x,  inset.y);
                
                var bottomL = new Vector3(cBL.x, center.y - half.y, 0f);
                var bottomR = new Vector3(cBR.x, center.y - half.y, 0f);
                var rightB  = new Vector3(center.x + half.x, cBR.y, 0f);
                var rightT  = new Vector3(center.x + half.x, cTR.y, 0f);
                var topR    = new Vector3(cTR.x, center.y + half.y, 0f);
                var topL    = new Vector3(cTL.x, center.y + half.y, 0f);
                var leftT   = new Vector3(center.x - half.x, cTL.y, 0f);
                var leftB   = new Vector3(center.x - half.x, cBL.y, 0f);

                Gizmos.DrawLine(bottomL, bottomR);
                Gizmos.DrawLine(rightB, rightT);
                Gizmos.DrawLine(topR, topL);
                Gizmos.DrawLine(leftT, leftB);
                
                DrawArc(cBL, radius, 180f, 270f, GIZMO_CORNER_SEGMENTS);
                DrawArc(cBR, radius, 270f, 360f, GIZMO_CORNER_SEGMENTS);
                DrawArc(cTR, radius,   0f,  90f, GIZMO_CORNER_SEGMENTS);
                DrawArc(cTL, radius,  90f, 180f, GIZMO_CORNER_SEGMENTS);
            }

            Gizmos.matrix = prevMatrix;
            Gizmos.color = prevColor;
        }

        private static void DrawArc(Vector2 center, float radius, float startDeg, float endDeg, int segments)
        {
            var prev = ArcPoint(center, radius, startDeg);
            for (var i = 1; i <= segments; i++)
            {
                var t = (float)i / segments;
                var deg = Mathf.Lerp(startDeg, endDeg, t);
                var cur = ArcPoint(center, radius, deg);
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }

        private static Vector3 ArcPoint(Vector2 center, float radius, float deg)
        {
            var rad = deg * Mathf.Deg2Rad;
            return new Vector3(center.x + Mathf.Cos(rad) * radius,
                center.y + Mathf.Sin(rad) * radius, 0f);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RoundedRectRaycastTarget), true)]
    [CanEditMultipleObjects]
    public class RoundedRectRaycastTargetEditor : NativeWrapperOdinEditor<MaskableGraphic, GraphicEditor>
    {
        protected override BaseEditorDrawMode BaseEditorDrawMode => BaseEditorDrawMode.Hidden;
    }
#endif
}