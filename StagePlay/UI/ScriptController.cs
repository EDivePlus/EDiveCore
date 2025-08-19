// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using DG.Tweening;
using EDIVE.XRTools;
using EnhancedUI.EnhancedScroller;
using Sirenix.OdinInspector;
using UnityEngine;
using Tween = DG.Tweening.Tween;

namespace EDIVE.StagePlay.UI
{
    public class ScriptController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        [SerializeField]
        private StagePlayDefinition _Definition;
        
        [SerializeField]
        private SmoothCameraFollower _CameraFollower;
        
        [SerializeField]
        private bool _OpenOnAwake = true;
        
        [SerializeField]
        private float _TweenDuration = 0.3f;

        [SerializeField]
        private GameObject _TabletRoot;

        [SerializeField]
        private EnhancedScroller _Scroller;

        [SerializeField]
        private LineScriptSegmentDisplay _LineDisplayPrefab;
        
        [SerializeField]
        private DirectionScriptSegmentDisplay _DirectionDisplayPrefab;
        
        
        [ShowInInspector]
        public bool IsOpen
        {
            get => _isOpen;
            set => SetOpen(value);
        }
        
        private Tween _animTween;
        private bool _isOpen = true;
        
        private List<AScriptSegment> _currentSegments;
        private int _currentSegmentIndex;
        private AScriptSegment _currentSegment;
        
        protected void Awake()
        {
            _isOpen = true;
        }
        
        private void Start()
        {
            _Scroller.Delegate = this;
            RefreshScroller();
        }

        private void RefreshScroller()
        {
            _Scroller.ReloadData(_Scroller.NormalizedScrollPosition);
        }

        [Button]
        public void JumpToCurrentSegment()
        {
            var jumpSegmentIndex = _currentSegments.IndexOf(_currentSegment);
            _Scroller.JumpToDataIndex(jumpSegmentIndex);
        }
        
        [Button]
        public void ToggleTablet()
        {
            _isOpen = !_isOpen;
            SetOpen(_isOpen);
        }
        
        public void SetOpen(bool open)
        {
            _animTween?.Kill();
            _isOpen = open;

            if (_CameraFollower != null)
            {
                _CameraFollower.Reposition();
                _CameraFollower.SetFollowing(open);
            }
            
            var newScale = open ? Vector3.one : Vector3.zero;
            _animTween = transform.DOScale(newScale, _TweenDuration).SetEase(Ease.InOutQuad);
        }
        
        public int GetNumberOfCells(EnhancedScroller scroller) => _currentSegments?.Count ?? 0;
        
        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            var rect = _currentSegments[dataIndex] switch
            {
                LineScriptSegment => (RectTransform) _LineDisplayPrefab?.transform,
                DirectionScriptSegment => (RectTransform) _DirectionDisplayPrefab?.transform,
                _ => null
            };
            
            if (rect != null) return rect.rect.height;
            return 0;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = _currentSegments[dataIndex] switch
            {
                LineScriptSegment _ => _LineDisplayPrefab ? _Scroller.GetCellView(_LineDisplayPrefab) as AScriptSegmentDisplay : null,
                DirectionScriptSegment _ => _DirectionDisplayPrefab ? _Scroller.GetCellView(_DirectionDisplayPrefab) as AScriptSegmentDisplay : null,
                _ => null
            };

            if (!cellView)
                return null;
            
            cellView.SetData(_currentSegments[dataIndex]);
            return cellView;
        }
    }
}
