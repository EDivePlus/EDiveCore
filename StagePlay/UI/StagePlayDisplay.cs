// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using DG.Tweening;
using EDIVE.Utils.Activations;
using EDIVE.XRTools;
using EnhancedUI.EnhancedScroller;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Tween = DG.Tweening.Tween;

namespace EDIVE.StagePlay.UI
{
    public class StagePlayDisplay : MonoBehaviour, IEnhancedScrollerDelegate
    {
        [Required]
        [SerializeField]
        private StagePlayController _Controller;
        
        [SerializeField]
        private SmoothCameraFollower _CameraFollower;
        
        [SerializeField]
        private bool _OpenOnStart = true;
        
        [SerializeField]
        private float _TweenDuration = 0.3f;

        [SerializeField]
        private EnhancedScroller _Scroller;

        [SerializeField]
        private LinePlaySegmentDisplay _LineDisplayPrefab;
        
        [SerializeField]
        private DirectionPlaySegmentDisplay _DirectionDisplayPrefab;
        
        [SerializeReference]
        private IActivation _ScrollToCurrentActivation;

        [SerializeField]
        private TMP_Text _NameText;
        
        [DisableInEditorMode]
        [ShowInInspector]
        public bool IsOpen
        {
            get => _isOpen;
            set => SetOpen(value);
        }
    
        private Tween _animTween;
        private bool _isOpen;
        private List<ASegmentDisplayData> _segmentsList;
        
        private StagePlayDefinition _currentDefinition;
        private StagePlayState _currentState;
        
        protected void Awake()
        {
            if (_Controller == null)
                return;
            
            _Scroller.Delegate = this;
            _Scroller.cellViewWillRecycle = OnCellViewWillRecycle;
            _ScrollToCurrentActivation?.RegisterActivationListener(OnScrollToCurrentActivated);
            _Controller.DefinitionChanged += UpdateDefinition;
        }

        private void OnDestroy()
        {
            _ScrollToCurrentActivation?.UnregisterActivationListener(OnScrollToCurrentActivated);
        }

        private void Start()
        {
            if (_Controller == null)
                return;
            
            if (_OpenOnStart)
                SetOpen(true);
            
            UpdateDefinition(_Controller.Definition, _Controller.CurrentState);
        }

        private void UpdateDefinition(StagePlayDefinition definition, StagePlayState state)
        {
            if (definition == null || state == null) 
                return;
            
            _currentDefinition = definition;
            _currentState = state;
            
            if(_NameText)
                _NameText.text = _currentDefinition.Name;
            
            _segmentsList ??= new List<ASegmentDisplayData>();
            _segmentsList.Clear();

            if (definition != null)
            {
                var index = 0;
                foreach (var segment in definition.ScriptSegments)
                {
                    ASegmentDisplayData segmentData = segment switch
                    {
                        SpeachPlaySegment lineSegment => new SegmentDisplayData<SpeachPlaySegment>(index, lineSegment, state),
                        DirectionPlaySegment directionSegment => new SegmentDisplayData<DirectionPlaySegment>(index, directionSegment, state),
                        _ => null
                    };

                    if (segmentData == null) 
                        continue;
                
                    _segmentsList.Add(segmentData);
                    index++;
                }
            }
            _Scroller.ReloadData(_Scroller.NormalizedScrollPosition);
        }
        
        [Button]
        public void JumpToCurrentSegment()
        {
            _Scroller.JumpToDataIndex(_currentState.CurrentSegmentIndex, 0.5f, 0.5f, true, EnhancedScroller.TweenType.easeOutBack, 0.3f);
        }
        
        [Button]
        public void ToggleTablet()
        {
            _isOpen = !_isOpen;
            SetOpen(_isOpen);
        }
        
        private void OnScrollToCurrentActivated()
        {
            JumpToCurrentSegment();
        }
        
        public void SetOpen(bool open, bool immediate = false)
        {
            _animTween?.Kill();
            _isOpen = open;

            if (_CameraFollower != null && open)
            {
                _CameraFollower.Reposition(immediate);
            }
            
            var newScale = open ? Vector3.one : Vector3.zero;
            if (immediate)
                transform.localScale = newScale;
            else
                _animTween = transform.DOScale(newScale, _TweenDuration).SetEase(Ease.InOutQuad);
        }
        
        private static void OnCellViewWillRecycle(EnhancedScrollerCellView cellView)
        {
            if (cellView is APlaySegmentDisplay display) 
                display.Clear();
        }
        
        public int GetNumberOfCells(EnhancedScroller scroller) => _segmentsList?.Count ?? 0;
        
        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            var rect = _segmentsList[dataIndex] switch
            {
                SegmentDisplayData<SpeachPlaySegment> => (RectTransform) _LineDisplayPrefab?.transform,
                SegmentDisplayData<DirectionPlaySegment> => (RectTransform) _DirectionDisplayPrefab?.transform,
                _ => null
            };
            
            if (rect != null) return rect.rect.height;
            return 0;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = _segmentsList[dataIndex] switch
            {
                SegmentDisplayData<SpeachPlaySegment> _ => _LineDisplayPrefab ? _Scroller.GetCellView(_LineDisplayPrefab) as APlaySegmentDisplay : null,
                SegmentDisplayData<DirectionPlaySegment> _ => _DirectionDisplayPrefab ? _Scroller.GetCellView(_DirectionDisplayPrefab) as APlaySegmentDisplay : null,
                _ => null
            };

            if (!cellView)
                return null;
            
            cellView.SetData(_segmentsList[dataIndex]);
            return cellView;
        }
    }
}
