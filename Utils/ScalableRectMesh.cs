// Author: František Holubec
// Created: 18.06.2025

using EDIVE.DataStructures;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils
{
    public class ScalableRectMesh : MonoBehaviour
    {
        [SerializeField]
        private MatchMode _MatchMode = MatchMode.RectTransform;

        [ShowIf(nameof(_MatchMode), MatchMode.RectTransform)]
        [SerializeField]
        private RectTransform _RectTransform;

        [ShowIf(nameof(_MatchMode), MatchMode.ManualSize)]
        [SerializeField]
        private Vector2 _ManualSize = Vector2.one;

        [SerializeField]
        private RectPadding _Padding;

        [SerializeField]
        private Vector3 _XDirection = Vector3.right;

        [SerializeField]
        private Vector3 _YDirection = Vector3.up;

        [PropertySpace]
        [SerializeField]
        private Transform _TopRightExtend;

        [SerializeField]
        private Transform _TopLeftExtend;

        [SerializeField]
        private Transform _BottomRightExtend;

        [SerializeField]
        private Transform _BottomLeftExtend;

        private Vector2 GetSize()
        {
            return _MatchMode switch
            {
                MatchMode.RectTransform => GetRectTransformSize(),
                MatchMode.ManualSize => _ManualSize,
                _ => Vector2.one
            };
        }

        private Vector2 GetRectTransformSize()
        {
            if (_RectTransform == null)
                return Vector2.one;

            var corners = new Vector3[4];
            _RectTransform.GetWorldCorners(corners);
            var width = Vector3.Distance(corners[0], corners[3]);
            var height = Vector3.Distance(corners[0], corners[1]);
            return new Vector2(width, height);
        }

        private void OnValidate()
        {
            RefreshVisual();
        }

        [Button]
        private void RefreshVisual()
        {
            var halfSize = GetSize() / 2;
            var xDir = _XDirection.normalized;
            var yDir = _YDirection.normalized;

            if (_TopRightExtend)
                SetLocalPosition(_TopRightExtend, xDir * (halfSize.x + _Padding.Right) + yDir * (halfSize.y + _Padding.Top));
            if (_TopLeftExtend)
                SetLocalPosition(_TopLeftExtend, -xDir * (halfSize.x + _Padding.Left) + yDir * (halfSize.y + _Padding.Top));
            if (_BottomRightExtend)
                SetLocalPosition(_BottomRightExtend, xDir * (halfSize.x + _Padding.Right) - yDir * (halfSize.y + _Padding.Bottom));
            if (_BottomLeftExtend)
                SetLocalPosition(_BottomLeftExtend, -xDir * (halfSize.x + _Padding.Left)  - yDir * (halfSize.y + _Padding.Bottom));
        }

        private static void SetLocalPosition(Transform target, Vector3 position)
        {
            if (target == null || target.localPosition == position)
                return;

            target.localPosition = position;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(target);
#endif
        }

        private enum MatchMode
        {
            RectTransform,
            ManualSize
        }
    }
}
